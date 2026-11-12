/**
 * @file TerminationService.cs
 * @brief Carries out an approved termination requisition.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Events;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;

/// <summary>
/// Applies a termination when its requisition moves to Completed. The changes are made on tracked
/// entities and saved by the caller in one SaveChanges, so the termination is all-or-nothing:
/// <list type="bullet">
/// <item>employment status becomes Terminated (the staff number is kept and never reused);</item>
/// <item>assignments still open end today; assignments that have not started are removed;</item>
/// <item>roster entries that have not finished are Cancelled, removing duty eligibility;</item>
/// <item>a Staff.Terminated event is queued in the outbox.</item>
/// </list>
/// </summary>
public class TerminationService
{
    private readonly StaffRepository _staffRepository;
    private readonly AssignmentRepository _assignmentRepository;
    private readonly RosterRepository _rosterRepository;
    private readonly OutboxWriter _outbox;
    private readonly Clock _clock;

    /// <summary>Creates the service.</summary>
    /// <param name="staffRepository">Staff data access.</param>
    /// <param name="assignmentRepository">Assignment data access.</param>
    /// <param name="rosterRepository">Roster data access.</param>
    /// <param name="outbox">Queues the Staff.Terminated event.</param>
    /// <param name="clock">Current UTC time.</param>
    public TerminationService(StaffRepository staffRepository, AssignmentRepository assignmentRepository,
        RosterRepository rosterRepository, OutboxWriter outbox, Clock clock)
    {
        _staffRepository = staffRepository;
        _assignmentRepository = assignmentRepository;
        _rosterRepository = rosterRepository;
        _outbox = outbox;
        _clock = clock;
    }

    /// <summary>Terminates the requisition's employee. Does not save; the caller does.</summary>
    /// <param name="requisition">A Termination requisition with its Staff loaded.</param>
    /// <returns>How many roster entries were cancelled.</returns>
    public async Task<int> ApplyAsync(Requisition requisition)
    {
        var staff = await _staffRepository.GetByNumberAsync(requisition.Staff!.StaffNumber, withDetails: true)
                    ?? throw new InvalidOperationException($"Staff {requisition.Staff.StaffNumber} disappeared.");
        var today = _clock.Today;
        var now = _clock.UtcNow;

        staff.EmploymentStatus = EmploymentStatus.Terminated;

        foreach (var assignment in staff.Assignments.ToList())
        {
            if (assignment.StartDate >= today)
                _assignmentRepository.Remove(assignment);       // never started
            else if (assignment.EndDate == null || assignment.EndDate > today)
                assignment.EndDate = today;                      // last day is today
        }

        var shifts = await _rosterRepository.ListUnfinishedForStaffAsync(staff.StaffId, now);
        foreach (var shift in shifts)
            shift.Status = RosterStatus.Cancelled;

        requisition.PositionsFilled = requisition.Positions;

        _outbox.Enqueue(IntegrationEventTypes.StaffTerminated, new StaffTerminatedEvent(
            staff.StaffNumber, staff.HomeDepartment.Code, today, requisition.RequisitionNumber, shifts.Count));

        return shifts.Count;
    }
}
