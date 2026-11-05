namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

/// <summary>Envelope for paged lists.</summary>
/// <param name="Items">The rows on this page.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Maximum rows per page.</param>
/// <param name="Total">Rows matching the filter across all pages.</param>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

/// <summary>Short reference to a department.</summary>
public record DepartmentRefDto(string Code, string Name);

/// <summary>Response body of GET /health.</summary>
public record HealthDto(string Status, IReadOnlyDictionary<string, string> Checks);
