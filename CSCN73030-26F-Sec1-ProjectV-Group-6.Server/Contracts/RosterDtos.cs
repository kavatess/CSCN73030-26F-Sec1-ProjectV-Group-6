using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

/// <summary>Body of POST /rosters/entries. Times are UTC; a value without an offset is read as UTC.</summary>
/// <param name="Status">Defaults to Scheduled.</param>
[RequiredFields("staffNumber", "serviceId", "shiftStart", "shiftEnd", "shiftType")]
public record CreateRosterEntryRequest(
    string? StaffNumber,
    int? ServiceId,
    DateTime? ShiftStart,
    DateTime? ShiftEnd,
    ShiftType? ShiftType,
    RosterStatus? Status);

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
/// <param name="Active">Staff with a current assignment in the service and status Active or Probation.</param>
/// <param name="OnDuty">Distinct staff on a non-cancelled shift in the service right now.</param>
public record ServiceHeadcountDto(int ServiceId, string ServiceName, string DepartmentCode, int Active, int OnDuty);
