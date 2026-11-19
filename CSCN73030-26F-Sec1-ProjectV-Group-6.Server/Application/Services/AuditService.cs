/**
 * @file AuditService.cs
 * @brief Reads the audit trail.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Validation;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;

/// <summary>Searches the audit trail written by the AuditInterceptor.</summary>
public class AuditService
{
    private readonly AuditRepository _auditRepository;

    /// <summary>Creates the service.</summary>
    /// <param name="auditRepository">Audit log data access.</param>
    public AuditService(AuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    /// <summary>Audit records matching the query, newest first.</summary>
    /// <param name="query">Filters and paging.</param>
    /// <exception cref="Errors.ValidationFailedException">Paging is out of range (422).</exception>
    public async Task<PagedResult<AuditLogDto>> SearchAsync(AuditQuery query)
    {
        var errors = new ValidationErrors();
        StaffService.ValidatePaging(errors, query.Page, query.PageSize);
        errors.ThrowIfAny();

        var (items, total) = await _auditRepository.SearchAsync(query.EntityType, query.EntityId, query.ActorId, query.Page, query.PageSize);
        return new PagedResult<AuditLogDto>(
            items.Select(a => new AuditLogDto(a.AuditId, a.EntityType, a.EntityId, a.Action, a.ActorId, a.TimestampUtc)).ToList(),
            query.Page, query.PageSize, total);
    }
}
