/**
 * @file AuditDtos.cs
 * @brief Request and response shapes for GET /audit.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

/// <summary>Query string of GET /audit. Every filter is optional.</summary>
public class AuditQuery
{
    /// <summary>Entity type, e.g. Staff, StaffAssignment, RosterEntry, Requisition, Credential, SecurityFlag, Compensation.</summary>
    public string? EntityType { get; set; }

    /// <summary>The entity's public id: a staff number, requisition number or numeric id.</summary>
    public string? EntityId { get; set; }

    /// <summary>Who made the change.</summary>
    public string? ActorId { get; set; }

    /// <summary>1-based page number. Defaults to 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>1 to 100. Defaults to 50.</summary>
    public int PageSize { get; set; } = 50;
}

/// <summary>One audit record.</summary>
/// <param name="AuditId">Audit record id; higher is newer.</param>
/// <param name="EntityType">Kind of record changed.</param>
/// <param name="EntityId">Public id of the record changed.</param>
/// <param name="Action">Created, Updated or Deleted.</param>
/// <param name="ActorId">Who made the change.</param>
/// <param name="TimestampUtc">When, UTC.</param>
public record AuditLogDto(long AuditId, string EntityType, string EntityId, string Action, string ActorId, DateTime TimestampUtc);
