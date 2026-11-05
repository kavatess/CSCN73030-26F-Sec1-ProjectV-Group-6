using System.Net.Mail;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Validation;

/// <summary>
/// Collects field-level validation messages, then throws one 422 with all of them.
/// Field names are camelCase to match the JSON the client sent.
/// </summary>
/// <example>
/// var errors = new ValidationErrors();
/// errors.Required("firstName", request.FirstName);
/// errors.ThrowIfAny();
/// </example>
public class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public bool HasErrors => _errors.Count > 0;

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var list))
        {
            list = [];
            _errors[field] = list;
        }
        list.Add(message);
    }

    /// <summary>Adds an error when <paramref name="condition"/> is true.</summary>
    public void AddIf(bool condition, string field, string message)
    {
        if (condition) Add(field, message);
    }

    /// <summary>Value must be non-blank and no longer than <paramref name="maxLength"/>.</summary>
    public void Required(string field, string? value, int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(value)) Add(field, "This field is required.");
        else MaxLength(field, value, maxLength);
    }

    public void Required<T>(string field, T? value) where T : struct
    {
        if (value == null) Add(field, "This field is required.");
    }

    public void MaxLength(string field, string? value, int maxLength)
    {
        if (value != null && value.Trim().Length > maxLength)
            Add(field, $"Must be at most {maxLength} characters.");
    }

    /// <summary>For PATCH: a field that was supplied must not be blank.</summary>
    public void NotBlankIfPresent(string field, string? value, int maxLength = 100)
    {
        if (value == null) return;
        Required(field, value, maxLength);
    }

    public void Email(string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        MaxLength(field, value, 254);
        if (!MailAddress.TryCreate(value.Trim(), out var parsed) || parsed.Address != value.Trim())
            Add(field, "Must be a valid email address.");
    }

    public void ThrowIfAny()
    {
        if (HasErrors)
            throw new ValidationFailedException(_errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
    }
}
