using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;

public class AssignmentRepository(CmesDbContext db)
{
    public Task<List<StaffAssignment>> ListForStaffAsync(int staffId) =>
        db.StaffAssignments
            .Include(a => a.Staff)
            .Include(a => a.Service).ThenInclude(s => s.Department)
            .Where(a => a.StaffId == staffId)
            .OrderByDescending(a => a.StartDate)
            .AsNoTracking()
            .ToListAsync();

    public Task<StaffAssignment?> GetByIdAsync(int assignmentId) =>
        db.StaffAssignments
            .Include(a => a.Staff)
            .Include(a => a.Service).ThenInclude(s => s.Department)
            .SingleOrDefaultAsync(a => a.AssignmentId == assignmentId);

    public void Add(StaffAssignment assignment) => db.StaffAssignments.Add(assignment);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
