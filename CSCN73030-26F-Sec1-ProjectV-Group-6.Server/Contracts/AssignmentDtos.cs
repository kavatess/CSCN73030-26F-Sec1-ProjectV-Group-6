/**
 * @file AssignmentDtos.cs
 * @brief Request and response shapes for assignment endpoints.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

/// <summary>Body of POST /staff/{staffNumber}/assignments (also used as CreateStaffRequest.InitialAssignment).</summary>
/// <param name="ServiceId">Required. Service the role is in.</param>
/// <param name="RoleTitle">Required. Job title, max 100 characters.</param>
/// <param name="StartDate">Defaults to today (or, for an initial assignment, the hire date).</param>
/// <param name="EndDate">Optional; must be after StartDate.</param>
[RequiredFields("serviceId", "roleTitle")]
public record CreateAssignmentRequest(int? ServiceId, string? RoleTitle, DateOnly? StartDate, DateOnly? EndDate);

/// <summary>Body of PATCH /assignments/{id}. Setting EndDate closes the assignment.</summary>
/// <param name="EndDate">Required. Last day of the assignment; must be after its StartDate.</param>
[RequiredFields("endDate")]
public record UpdateAssignmentRequest(DateOnly? EndDate);

/// <summary>An assignment as returned by the API.</summary>
/// <param name="AssignmentId">Id used by PATCH /assignments/{id}.</param>
/// <param name="StaffNumber">Assigned staff member.</param>
/// <param name="ServiceId">Service id.</param>
/// <param name="ServiceName">Service name.</param>
/// <param name="DepartmentCode">Code of the service's department.</param>
/// <param name="RoleTitle">Job title.</param>
/// <param name="StartDate">First day.</param>
/// <param name="EndDate">Last day, or null while open-ended.</param>
/// <param name="IsCurrent">True when the assignment covers today.</param>
public record AssignmentDto(
    int AssignmentId,
    string StaffNumber,
    int ServiceId,
    string ServiceName,
    string DepartmentCode,
    string RoleTitle,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsCurrent);
