/**
 * @file BudgetOptions.cs
 * @brief Configuration for calling the Budget and Treasury service (section "Budget").
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Budget;

/// <summary>
/// Where the Budget and Treasury (Finance) API lives and how Staff/HR talks to it. With no <see cref="BaseUrl"/>
/// the integration is off: funding decisions are recorded by hand, as before.
/// </summary>
public class BudgetOptions
{
    /// <summary>Configuration section name.</summary>
    public const string Section = "Budget";

    /// <summary>Base address of the Finance API, e.g. <c>https://finance.example/</c>. Empty = integration off.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>How long one call may take. The Finance API can be slow to wake up, so this is generous.</summary>
    public int TimeoutSeconds { get; set; } = 100;

    /// <summary>Whether a background loop checks Budget for funding decisions.</summary>
    public bool BackgroundSync { get; set; } = true;

    /// <summary>Minutes between background checks for funding decisions.</summary>
    public int SyncIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Staff/HR department code → the code Budget uses for the same department. Codes not listed are sent
    /// unchanged. Values from configuration are added to (and override) these defaults.
    /// </summary>
    public Dictionary<string, string> DepartmentCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["EMS"] = "EMS",
        ["SEC"] = "SEC",
        ["PTR"] = "TRANSIT",
        ["MNT"] = "UTIL",
        ["BUD"] = "FIN",
        ["RES"] = "RES",
        ["HRS"] = "HR",
    };

    /// <summary>True when a base address is set.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl);

    /// <summary>The Budget code for one of our department codes.</summary>
    public string ToBudgetCode(string departmentCode) =>
        DepartmentCodes.TryGetValue(departmentCode, out var code) && !string.IsNullOrWhiteSpace(code)
            ? code.Trim()
            : departmentCode;
}
