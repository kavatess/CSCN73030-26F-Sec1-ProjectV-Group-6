/**
 * @file FakeBudgetApi.cs
 * @brief In-memory stand-in for the Budget and Treasury (Finance) API, used as the BudgetClient's HTTP handler.
 *
 * It answers the routes Staff/HR calls, with the JSON shapes of the Finance API's OpenAPI document:
 * funding requests (status as a number: 0 Pending, 1 Approved, 2 Rejected), department budgets and expenses.
 */

using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>A fake Finance API. Tests decide funding requests and can make every call fail.</summary>
    public sealed class FakeBudgetApi : HttpMessageHandler
    {
        public const string BaseUrl = "https://budget.test/";

        private readonly ConcurrentDictionary<Guid, JsonObject> _fundingRequests = new();
        private readonly ConcurrentDictionary<string, decimal> _allocated = new(StringComparer.OrdinalIgnoreCase)
        {
            ["EMS"] = 4_500_000m, ["SEC"] = 5_000_000m, ["TRANSIT"] = 4_000_000m, ["UTIL"] = 3_500_000m,
            ["FIN"] = 600_000m, ["RES"] = 800_000m, ["HR"] = 1_200_000m,
        };
        private readonly ConcurrentDictionary<string, decimal> _spent = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Every request received, as "METHOD /path" plus its JSON body.</summary>
        public ConcurrentQueue<(string Call, JsonNode? Body)> Calls { get; } = new();

        /// <summary>When set, every call answers this status with an { error, message } body.</summary>
        public HttpStatusCode? FailWith { get; set; }

        /// <summary>Records Budget's decision on a funding request (what Budget's own PUT .../decision does).</summary>
        public void Decide(Guid id, bool approved)
        {
            var request = _fundingRequests[id];
            request["status"] = approved ? 1 : 2;
            request["decidedAtUtc"] = "2026-11-06T09:30:00Z";
        }

        /// <summary>The funding requests created so far.</summary>
        public IReadOnlyCollection<JsonObject> FundingRequests => _fundingRequests.Values.ToList();

        /// <summary>Sets how much a department has already spent.</summary>
        public void SetSpent(string budgetCode, decimal spent) => _spent[budgetCode] = spent;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath.TrimEnd('/');
            JsonNode? body = request.Content == null ? null : JsonNode.Parse(await request.Content.ReadAsStringAsync(ct));
            Calls.Enqueue(($"{request.Method} {path}", body));

            if (FailWith is { } failure)
                return Reply(failure, new JsonObject { ["error"] = "unavailable", ["message"] = "Budget is down for maintenance" });

            if (request.Method == HttpMethod.Post && path == "/api/budget/v1/funding-requests")
            {
                var id = Guid.NewGuid();
                var created = new JsonObject
                {
                    ["id"] = id.ToString(),
                    ["departmentCode"] = body!["departmentCode"]!.GetValue<string>(),
                    ["amount"] = body["amount"]!.GetValue<decimal>(),
                    ["reason"] = body["reason"]?.GetValue<string>(),
                    ["urgent"] = body["urgent"]?.GetValue<bool>() ?? false,
                    ["createdAtUtc"] = "2026-11-05T12:00:00Z",
                    ["status"] = 0,
                    ["decidedAtUtc"] = null,
                };
                _fundingRequests[id] = created;
                return Reply(HttpStatusCode.OK, created);
            }
            if (request.Method == HttpMethod.Get && path == "/api/budget/v1/funding-requests")
                return Reply(HttpStatusCode.OK, new JsonArray(_fundingRequests.Values.Select(r => (JsonNode)r.DeepClone()).ToArray()));

            if (request.Method == HttpMethod.Get && path.StartsWith("/api/budget/v1/budgets/"))
            {
                var code = Uri.UnescapeDataString(path["/api/budget/v1/budgets/".Length..]);
                if (!_allocated.TryGetValue(code, out var allocated))
                    return Reply(HttpStatusCode.NotFound, new JsonObject { ["error"] = "not_found", ["message"] = $"No budget for {code}" });
                var spent = _spent.GetValueOrDefault(code);
                return Reply(HttpStatusCode.OK, new JsonObject
                {
                    ["departmentCode"] = code.ToUpperInvariant(), ["departmentName"] = code, ["allocated"] = allocated,
                    ["spent"] = spent, ["remaining"] = allocated - spent, ["lowBudget"] = allocated - spent < allocated / 10,
                });
            }

            if (request.Method == HttpMethod.Post && path == "/api/budget/v1/expenses")
            {
                var code = body!["departmentCode"]!.GetValue<string>();
                var amount = body["amount"]!.GetValue<decimal>();
                var remaining = _allocated.GetValueOrDefault(code) - _spent.GetValueOrDefault(code);
                if (amount > remaining)
                    return Reply(HttpStatusCode.OK, new JsonObject
                    {
                        ["approved"] = false, ["message"] = "Insufficient budget", ["remainingBudget"] = remaining, ["expense"] = null,
                    });
                _spent[code] = _spent.GetValueOrDefault(code) + amount;
                return Reply(HttpStatusCode.OK, new JsonObject
                {
                    ["approved"] = true, ["message"] = "Expense recorded", ["remainingBudget"] = remaining - amount,
                    ["expense"] = new JsonObject
                    {
                        ["id"] = Guid.NewGuid().ToString(), ["departmentCode"] = code, ["amount"] = amount,
                        ["description"] = body["description"]?.GetValue<string>(), ["createdAtUtc"] = "2026-11-05T12:00:00Z",
                    },
                });
            }

            return Reply(HttpStatusCode.NotFound, new JsonObject { ["error"] = "not_found", ["message"] = $"No route {path}" });
        }

        private static HttpResponseMessage Reply(HttpStatusCode status, JsonNode body) => new(status)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
    }
}
