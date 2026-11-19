/**
 * @file Mapping.cs
 * @brief Entity to DTO conversions.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application;

/// <summary>
/// Entity → DTO conversions. Callers must have loaded the navigation properties used here
/// (the repositories' Include calls do this).
/// </summary>
/// <remarks>Business rule 4: nothing here reads Compensation. Keep it that way.</remarks>
public static class Mapping
{
    /// <summary>Department to its code and name.</summary>
    /// <param name="department">Loaded department.</param>
    public static DepartmentRefDto ToRef(this Department department) => new(department.Code, department.Name);

    /// <summary>Staff to the full profile. Needs HomeDepartment, Assignments (with Service and Department) and Credentials (with CredentialType) loaded.</summary>
    /// <param name="staff">Loaded staff member.</param>
    /// <param name="today">Today (UTC), for current assignments and credential status.</param>
    public static StaffProfileDto ToProfile(this Staff staff, DateOnly today) => new(
        staff.StaffNumber,
        staff.DisplayName,
        staff.FirstName,
        staff.LastName,
        staff.PreferredName,
        staff.Email,
        staff.Phone,
        staff.DateOfBirth,
        staff.CitizenId,
        staff.HireDate,
        staff.EmploymentStatus,
        staff.EmploymentType,
        staff.HomeDepartment.ToRef(),
        CredentialStatusRules.Worst(staff.Credentials.Select(c => CredentialStatusRules.Derive(c, today))),
        staff.Assignments
            .OrderByDescending(a => a.IsCurrentOn(today))
            .ThenByDescending(a => a.StartDate)
            .Select(a => a.ToDto(today))
            .ToList(),
        staff.Credentials
            .OrderBy(c => c.ExpiryDate)
            .Select(c => c.ToDto(today))
            .ToList());

    /// <summary>Staff to a directory row. Needs HomeDepartment, Assignments (with Service) and Credentials loaded.</summary>
    /// <param name="staff">Loaded staff member.</param>
    /// <param name="today">Today (UTC).</param>
    public static StaffSummaryDto ToSummary(this Staff staff, DateOnly today)
    {
        var current = staff.Assignments
            .Where(a => a.IsCurrentOn(today))
            .OrderByDescending(a => a.StartDate)
            .FirstOrDefault();

        return new StaffSummaryDto(
            staff.StaffNumber,
            staff.DisplayName,
            staff.FirstName,
            staff.LastName,
            staff.Email,
            staff.Phone,
            staff.EmploymentStatus,
            staff.EmploymentType,
            staff.HomeDepartment.ToRef(),
            current?.RoleTitle,
            current?.Service.Name,
            CredentialStatusRules.Worst(staff.Credentials.Select(c => CredentialStatusRules.Derive(c, today))));
    }

    /// <summary>Assignment to its DTO. Needs Staff and Service (with Department) loaded.</summary>
    /// <param name="assignment">Loaded assignment.</param>
    /// <param name="today">Today (UTC), for IsCurrent.</param>
    public static AssignmentDto ToDto(this StaffAssignment assignment, DateOnly today) => new(
        assignment.AssignmentId,
        assignment.Staff.StaffNumber,
        assignment.ServiceId,
        assignment.Service.Name,
        assignment.Service.Department.Code,
        assignment.RoleTitle,
        assignment.StartDate,
        assignment.EndDate,
        assignment.IsCurrentOn(today));

    /// <summary>Credential to its DTO with the derived status (rule 5). Needs Staff (with HomeDepartment) and CredentialType loaded.</summary>
    /// <param name="credential">Loaded credential.</param>
    /// <param name="today">Today (UTC).</param>
    public static CredentialDto ToDto(this Credential credential, DateOnly today) => new(
        credential.CredentialId,
        credential.Staff.StaffNumber,
        credential.Staff.DisplayName,
        credential.Staff.HomeDepartment.Code,
        credential.CredentialTypeId,
        credential.CredentialType.Name,
        credential.CredentialType.IsDutyBlocking,
        credential.Number,
        credential.IssueDate,
        credential.ExpiryDate,
        CredentialStatusRules.Derive(credential, today),
        credential.ExpiryDate.DayNumber - today.DayNumber);

    /// <summary>Roster entry to its DTO. Needs Staff and Service loaded.</summary>
    /// <param name="entry">Loaded roster entry.</param>
    public static RosterEntryDto ToDto(this RosterEntry entry) => new(
        entry.RosterEntryId,
        entry.Staff.StaffNumber,
        entry.Staff.DisplayName,
        entry.ServiceId,
        entry.Service.Name,
        entry.ShiftStart,
        entry.ShiftEnd,
        entry.ShiftType,
        entry.Status);

    /// <summary>Requisition to its DTO. Needs RequestingDepartment (and Staff, if set) loaded.</summary>
    /// <param name="requisition">Loaded requisition.</param>
    public static RequisitionDto ToDto(this Requisition requisition) => new(
        requisition.RequisitionNumber,
        requisition.Type,
        requisition.RequestingDepartment.ToRef(),
        requisition.RoleTitle,
        requisition.Positions,
        requisition.PositionsFilled,
        requisition.Status,
        requisition.ReasonCode,
        requisition.BudgetDecision,
        requisition.Staff?.StaffNumber,
        requisition.CreatedAtUtc);
}
