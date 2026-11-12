/**
 * @file AssignmentRepository.cs
 * @brief Database access for staff assignments.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;

/// <summary>EF Core queries and writes for <see cref="StaffAssignment"/>.</summary>
public class AssignmentRepository
{
    private readonly CmesDbContext _db;

    /// <summary>Creates the AssignmentRepository. Dependencies are supplied by dependency injection.</summary>
    /// <param name="db">Database context.</param>
    public AssignmentRepository(CmesDbContext db)
    {
        _db = db;
    }

    /// <summary>Every assignment of one staff member, newest first.</summary>
    /// <param name="staffId">Staff key.</param>
    public Task<List<StaffAssignment>> ListForStaffAsync(int staffId) =>
        _db.StaffAssignments
            .Include(a => a.Staff)
            .Include(a => a.Service).ThenInclude(s => s.Department)
            .Where(a => a.StaffId == staffId)
            .OrderByDescending(a => a.StartDate)
            .AsNoTracking()
            .ToListAsync();

    /// <summary>Finds an assignment with its staff member and service.</summary>
    /// <param name="assignmentId">Assignment id.</param>
    public Task<StaffAssignment?> GetByIdAsync(int assignmentId) =>
        _db.StaffAssignments
            .Include(a => a.Staff)
            .Include(a => a.Service).ThenInclude(s => s.Department)
            .SingleOrDefaultAsync(a => a.AssignmentId == assignmentId);

    /// <summary>Adds a new assignment to be inserted on the next save.</summary>
    /// <param name="assignment">The new assignment.</param>
    public void Add(StaffAssignment assignment) => _db.StaffAssignments.Add(assignment);

    /// <summary>Saves pending changes to the database.</summary>
    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
