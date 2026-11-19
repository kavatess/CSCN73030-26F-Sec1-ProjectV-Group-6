/**
 * @file RequiredFieldsAttribute.cs
 * @brief Attribute listing the required fields of a request body, for OpenAPI.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;

/// <summary>
/// Lists the JSON fields a request body must contain, for the OpenAPI document only.
/// The real checks live in the application services, which answer 422 with a field-level
/// error. (Request DTOs make every field nullable so a missing field reaches that validation
/// instead of failing JSON parsing with a 400.)
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class RequiredFieldsAttribute : Attribute
{
    /// <summary>Lists the required fields.</summary>
    /// <param name="fields">camelCase JSON field names.</param>
    public RequiredFieldsAttribute(params string[] fields)
    {
        Fields = fields;
    }

    /// <summary>camelCase names of the required JSON fields.</summary>
    public IReadOnlyList<string> Fields { get; }
}
