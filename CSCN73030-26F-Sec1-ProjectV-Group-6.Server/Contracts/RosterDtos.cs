/**
 * @file RosterDtos.cs
 * @brief Request and response shapes for the /rosters endpoints.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

/// <summary>Body of POST /rosters/entries. Times are UTC; a value without an offset is read as UTC.</summary>
/// <param name="StaffNumber">Required. Staff member to roster; may not be Terminated.</param>
/// <param name="ServiceId">Required. Service the shift is in.</param>
/// <param name="ShiftStart">Required. Shift start (inclusive).</param>
/// <param name="ShiftEnd">Required. Shift end (exclusive); after ShiftStart and at most 24 hours later.</param>
/// <param name="ShiftType">Required. Day, Evening, Night or OnCall.</param>
/// <param name="Status">Defaults to Scheduled.</param>
[RequiredFields("staffNumber", "serviceId", "shiftStart", "shiftEnd", "shiftType")]
public record CreateRosterEntryRequest(
    string? StaffNumber,
    int? ServiceId,
    DateTime? ShiftStart,
    DateTime? ShiftEnd,
    ShiftType? ShiftType,
    RosterStatus? Status);

/// <summary>A shift as returned by the API.</summary>
/// <param name="RosterEntryId">Id used by DELETE /rosters/entries/{id}.</param>
/// <param name="StaffNumber">Rostered staff member.</param>
/// <param name="StaffName">Their display name.</param>
/// <param name="ServiceId">Service id.</param>
/// <param name="ServiceName">Service name.</param>
/// <param name="ShiftStart">Start, UTC.</param>
/// <param name="ShiftEnd">End, UTC.</param>
/// <param name="ShiftType">Day, Evening, Night or OnCall.</param>
/// <param name="Status">Scheduled, Confirmed, Completed or Cancelled.</param>
public record RosterEntryDto(
    int RosterEntryId,
    string StaffNumber,
    string StaffName,
    int ServiceId,
    string ServiceName,
    DateTime ShiftStart,
    DateTime ShiftEnd,
    ShiftType ShiftType,
    RosterStatus Status);

/// <summary>Per-service numbers for the Duty roster screen.</summary>
/// <param name="ServiceId">Service id.</param>
/// <param name="ServiceName">Service name.</param>
/// <param name="DepartmentCode">Owning department's code.</param>
/// <param name="Active">Staff with a current assignment in the service and status Active or Probation.</param>
/// <param name="OnDuty">Distinct staff on a non-cancelled shift in the service right now.</param>
public record ServiceHeadcountDto(int ServiceId, string ServiceName, string DepartmentCode, int Active, int OnDuty);
