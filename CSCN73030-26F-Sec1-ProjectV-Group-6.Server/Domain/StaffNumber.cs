using System.Text.RegularExpressions;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>
/// Business rule 1: staff numbers look like STF-0000000 (prefix plus 7 zero-padded digits).
/// They carry no department code, so a transfer never changes them.
/// </summary>
public static partial class StaffNumber
{
    public const string Prefix = "STF-";
    public const int Length = 11; // "STF-" + 7 digits

    public static string Format(long sequenceValue) => $"{Prefix}{sequenceValue:D7}";

    public static bool IsWellFormed(string? value) => value != null && Pattern().IsMatch(value);

    [GeneratedRegex(@"^STF-\d{7}$")]
    private static partial Regex Pattern();
}
