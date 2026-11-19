/**
 * @file AuditRepository.cs
 * @brief Database access for the audit log.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;

/// <summary>EF Core queries for <see cref="AuditLog"/>. Rows are written by the AuditInterceptor, never here.</summary>
public class AuditRepository
{
    private readonly CmesDbContext _db;

    /// <summary>Creates the repository.</summary>
    /// <param name="db">Database context.</param>
    public AuditRepository(CmesDbContext db)
    {
        _db = db;
    }

    /// <summary>Audit records matching the optional filters, newest first.</summary>
    /// <param name="entityType">Entity type, or null.</param>
    /// <param name="entityId">Entity public id, or null.</param>
    /// <param name="actorId">Actor, or null.</param>
    /// <param name="page">1-based page.</param>
    /// <param name="pageSize">Rows per page.</param>
    public async Task<(List<AuditLog> Items, int Total)> SearchAsync(
        string? entityType, string? entityId, string? actorId, int page, int pageSize)
    {
        IQueryable<AuditLog> query = _db.AuditLogs;
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(a => a.EntityType == entityType.Trim());
        if (!string.IsNullOrWhiteSpace(entityId)) query = query.Where(a => a.EntityId == entityId.Trim());
        if (!string.IsNullOrWhiteSpace(actorId)) query = query.Where(a => a.ActorId == actorId.Trim());

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.AuditId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
        return (items, total);
    }
}
