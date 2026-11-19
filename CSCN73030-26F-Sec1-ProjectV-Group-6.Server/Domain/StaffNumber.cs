/**
 * @file StaffNumber.cs
 * @brief Format of the city-wide staff number (STF-0000000).
 */

using System.Text.RegularExpressions;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>
/// Staff numbers look like STF-0000000 (prefix plus 7 zero-padded digits).
/// They carry no department code, so a transfer never changes them.
/// </summary>
/// @par Business rule 1
/// Numbers come from one city-wide SQL sequence (see StaffRepository.NextStaffNumberAsync),
/// are assigned once on create and are never changed or reused.
public static partial class StaffNumber
{
    /// <summary>Fixed prefix of every staff number.</summary>
    public const string Prefix = "STF-";

    /// <summary>Total length: "STF-" plus 7 digits.</summary>
    public const int Length = 11;

    /// <summary>Formats a sequence value as a staff number.</summary>
    /// <param name="sequenceValue">Next value of the staff number sequence, from 1.</param>
    /// <returns>For example <c>Format(42)</c> returns "STF-0000042".</returns>
    public static string Format(long sequenceValue) => $"{Prefix}{sequenceValue:D7}";

    /// <summary>True when <paramref name="value"/> matches STF- followed by exactly 7 digits.</summary>
    /// <param name="value">Text to check; null is not well formed.</param>
    public static bool IsWellFormed(string? value) => value != null && Pattern().IsMatch(value);

    [GeneratedRegex(@"^STF-\d{7}$")]
    private static partial Regex Pattern();
}
