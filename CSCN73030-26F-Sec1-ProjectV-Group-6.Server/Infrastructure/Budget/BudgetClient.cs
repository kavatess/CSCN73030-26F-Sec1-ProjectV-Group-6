/**
 * @file BudgetClient.cs
 * @brief Typed HTTP client for the Budget and Treasury (Finance) API.
 *
 * Covers the calls Staff/HR makes (see the Finance API's /swagger): funding requests for new hires,
 * department budgets, and salary expense statements. Failures become a 502 or 503 ProblemDetails.
 */

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using Microsoft.Extensions.Options;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Budget;

/// <summary>Where a funding request stands in Budget.</summary>
public enum FundingStatus
{
    /// <summary>Waiting for Budget's decision.</summary>
    Pending,
    /// <summary>Budget funds it.</summary>
    Approved,
    /// <summary>Budget refused it.</summary>
    Rejected,
}

/// <summary>A funding request as Budget returns it.</summary>
public record BudgetFundingRequest(Guid Id, string? DepartmentCode, decimal Amount, string? Reason, bool Urgent,
    DateTime CreatedAtUtc, FundingStatus Status, DateTime? DecidedAtUtc);

/// <summary>A department's budget as Budget returns it.</summary>
public record BudgetDepartmentBudget(string? DepartmentCode, string? DepartmentName, decimal Allocated, decimal Spent,
    decimal Remaining, bool LowBudget);

/// <summary>Budget's answer to an expense: whether it was accepted, why not, and what is left.</summary>
public record BudgetExpenseResult(bool Approved, string? Message, decimal? RemainingBudget, Guid? ExpenseId);

/// <summary>Calls the Finance API. Registered as a typed <see cref="HttpClient"/>.</summary>
public class BudgetClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly BudgetOptions _options;
    private readonly ILogger<BudgetClient> _logger;

    /// <summary>Creates the client.</summary>
    public BudgetClient(HttpClient http, IOptions<BudgetOptions> options, ILogger<BudgetClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>True when <c>Budget:BaseUrl</c> is set.</summary>
    public bool IsConfigured => _options.IsConfigured;

    /// <summary>The Budget code for one of our department codes.</summary>
    public string ToBudgetCode(string departmentCode) => _options.ToBudgetCode(departmentCode);

    /// <summary>POST /api/budget/v1/funding-requests: asks Budget to fund an amount for a department.</summary>
    public async Task<BudgetFundingRequest> CreateFundingRequestAsync(string budgetDepartmentCode, decimal amount,
        string reason, bool urgent, CancellationToken ct = default)
    {
        var body = new { departmentCode = budgetDepartmentCode, amount, reason, urgent };
        var json = await SendAsync(HttpMethod.Post, "api/budget/v1/funding-requests", body, ct);
        return ToFundingRequest(json);
    }

    /// <summary>GET /api/budget/v1/funding-requests: every funding request Budget holds.</summary>
    public async Task<List<BudgetFundingRequest>> ListFundingRequestsAsync(CancellationToken ct = default)
    {
        var json = await SendAsync(HttpMethod.Get, "api/budget/v1/funding-requests", null, ct);
        if (json.ValueKind != JsonValueKind.Array)
            throw new BudgetServiceException("Budget answered the funding-request list with something other than a list.");
        return json.EnumerateArray().Select(ToFundingRequest).ToList();
    }

    /// <summary>GET /api/budget/v1/budgets/{code}: one department's allocation, spending and what is left; null if Budget has no such department.</summary>
    public async Task<BudgetDepartmentBudget?> GetDepartmentBudgetAsync(string budgetDepartmentCode, CancellationToken ct = default)
    {
        var json = await SendAsync(HttpMethod.Get, $"api/budget/v1/budgets/{Uri.EscapeDataString(budgetDepartmentCode)}", null, ct,
            notFoundIsNull: true);
        if (json.ValueKind == JsonValueKind.Undefined) return null;
        return new BudgetDepartmentBudget(
            Text(json, "departmentCode"), Text(json, "departmentName"),
            Number(json, "allocated"), Number(json, "spent"), Number(json, "remaining"),
            json.TryGetProperty("lowBudget", out var low) && low.ValueKind == JsonValueKind.True);
    }

    /// <summary>POST /api/budget/v1/expenses: records an expense against a department's budget.</summary>
    public async Task<BudgetExpenseResult> CreateExpenseAsync(string budgetDepartmentCode, decimal amount, string description,
        CancellationToken ct = default)
    {
        var body = new { departmentCode = budgetDepartmentCode, amount, description };
        var json = await SendAsync(HttpMethod.Post, "api/budget/v1/expenses", body, ct);
        Guid? expenseId = json.TryGetProperty("expense", out var expense) && expense.ValueKind == JsonValueKind.Object &&
                          expense.TryGetProperty("id", out var id) && id.TryGetGuid(out var guid) ? guid : null;
        decimal? remaining = json.TryGetProperty("remainingBudget", out var r) && r.ValueKind == JsonValueKind.Number ? r.GetDecimal() : null;
        return new BudgetExpenseResult(
            json.TryGetProperty("approved", out var approved) && approved.ValueKind == JsonValueKind.True,
            Text(json, "message"), remaining, expenseId);
    }

    private async Task<JsonElement> SendAsync(HttpMethod method, string path, object? body, CancellationToken ct,
        bool notFoundIsNull = false)
    {
        if (!IsConfigured) throw new BudgetNotConfiguredException();

        using var request = new HttpRequestMessage(method, path);
        if (body != null) request.Content = JsonContent.Create(body, options: Json);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Budget call {Method} {Path} failed", method, path);
            throw new BudgetServiceException($"The Budget service could not be reached ({ex.GetType().Name}). Try again in a minute.");
        }

        using (response)
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            if (notFoundIsNull && response.StatusCode == HttpStatusCode.NotFound) return default;
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Budget call {Method} {Path} answered {Status}: {Body}", method, path, (int)response.StatusCode, text);
                throw new BudgetServiceException($"The Budget service answered {(int)response.StatusCode}{ErrorMessage(text)}.");
            }
            try
            {
                return JsonDocument.Parse(text).RootElement.Clone();
            }
            catch (JsonException)
            {
                throw new BudgetServiceException("The Budget service answered with something that is not JSON.");
            }
        }
    }

    /// <summary>Reads Budget's status, which arrives as a number (0 Pending, 1 Approved, 2 Rejected) or a name.</summary>
    public static FundingStatus ParseStatus(JsonElement status) => status.ValueKind switch
    {
        JsonValueKind.Number => status.GetInt32() switch
        {
            1 => FundingStatus.Approved,
            2 => FundingStatus.Rejected,
            _ => FundingStatus.Pending,
        },
        JsonValueKind.String => Enum.TryParse<FundingStatus>(status.GetString(), ignoreCase: true, out var parsed)
            ? parsed
            : FundingStatus.Pending,
        _ => FundingStatus.Pending,
    };

    private static BudgetFundingRequest ToFundingRequest(JsonElement json)
    {
        if (json.ValueKind != JsonValueKind.Object || !json.TryGetProperty("id", out var id) || !id.TryGetGuid(out var guid))
            throw new BudgetServiceException("The Budget service answered without a funding request id.");
        return new BudgetFundingRequest(
            guid, Text(json, "departmentCode"), Number(json, "amount"), Text(json, "reason"),
            json.TryGetProperty("urgent", out var urgent) && urgent.ValueKind == JsonValueKind.True,
            Date(json, "createdAtUtc") ?? DateTime.MinValue,
            json.TryGetProperty("status", out var status) ? ParseStatus(status) : FundingStatus.Pending,
            Date(json, "decidedAtUtc"));
    }

    private static string? Text(JsonElement json, string name) =>
        json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static decimal Number(JsonElement json, string name) =>
        json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDecimal() : 0m;

    private static DateTime? Date(JsonElement json, string name) =>
        json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && value.TryGetDateTime(out var date)
            ? DateTime.SpecifyKind(date.Kind == DateTimeKind.Local ? date.ToUniversalTime() : date, DateTimeKind.Utc)
            : null;

    /// <summary>": message" from Budget's error body ({ error, message, traceId } or ProblemDetails), or "".</summary>
    private static string ErrorMessage(string text)
    {
        try
        {
            var json = JsonDocument.Parse(text).RootElement;
            var message = Text(json, "message") ?? Text(json, "detail") ?? Text(json, "title") ?? Text(json, "error");
            return string.IsNullOrWhiteSpace(message) ? "" : ": " + message;
        }
        catch (JsonException)
        {
            return "";
        }
    }
}
