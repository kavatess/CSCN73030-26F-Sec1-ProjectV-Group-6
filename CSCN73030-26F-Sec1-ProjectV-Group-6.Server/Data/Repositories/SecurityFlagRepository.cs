/**
 * @file SecurityFlagRepository.cs
 * @brief Database access for security flags.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;

/// <summary>EF Core queries and writes for <see cref="SecurityFlag"/>.</summary>
public class SecurityFlagRepository
{
    private readonly CmesDbContext _db;

    /// <summary>Creates the repository.</summary>
    /// <param name="db">Database context.</param>
    public SecurityFlagRepository(CmesDbContext db)
    {
        _db = db;
    }

    /// <summary>A staff member's flag of one type, if it exists.</summary>
    /// <param name="staffId">Staff key.</param>
    /// <param name="flagType">Flag type (case-insensitive in SQL Server).</param>
    public Task<SecurityFlag?> FindAsync(int staffId, string flagType) =>
        _db.SecurityFlags.Include(f => f.Staff)
            .SingleOrDefaultAsync(f => f.StaffId == staffId && f.FlagType == flagType);

    /// <summary>Every flag of one staff member, by type.</summary>
    /// <param name="staffId">Staff key.</param>
    public Task<List<SecurityFlag>> ListForStaffAsync(int staffId) =>
        _db.SecurityFlags.Include(f => f.Staff)
            .Where(f => f.StaffId == staffId)
            .OrderBy(f => f.FlagType)
            .AsNoTracking()
            .ToListAsync();

    /// <summary>Flags matching the optional filters, most recently changed first.</summary>
    /// <param name="status">Status to match, or null.</param>
    /// <param name="flagType">Flag type to match, or null.</param>
    public Task<List<SecurityFlag>> SearchAsync(SecurityFlagStatus? status, string? flagType) =>
        _db.SecurityFlags.Include(f => f.Staff)
            .Where(f => status == null || f.Status == status)
            .Where(f => flagType == null || f.FlagType == flagType)
            .OrderByDescending(f => f.UpdatedAt).ThenBy(f => f.FlagId)
            .AsNoTracking()
            .ToListAsync();

    /// <summary>Adds a new flag to be inserted on the next save.</summary>
    /// <param name="flag">The new flag.</param>
    public void Add(SecurityFlag flag) => _db.SecurityFlags.Add(flag);

    /// <summary>Saves pending changes to the database.</summary>
    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
