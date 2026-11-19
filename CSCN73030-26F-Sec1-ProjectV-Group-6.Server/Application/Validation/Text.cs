/**
 * @file Text.cs
 * @brief String helpers for copying request values onto entities.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Validation;

/// <summary>Small string helpers used when copying request values onto entities.</summary>
public static class Text
{
    /// <summary>Trims the value; returns null for null or whitespace.</summary>
    public static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Emails are compared case-insensitively, so they are stored lower-case.</summary>
    public static string NormaliseEmail(string value) => value.Trim().ToLowerInvariant();
}
