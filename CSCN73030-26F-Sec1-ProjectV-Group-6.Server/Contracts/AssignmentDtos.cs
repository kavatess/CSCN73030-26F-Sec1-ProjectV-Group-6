using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

/// <summary>Body of POST /staff/{staffNumber}/assignments.</summary>
/// <param name="StartDate">Defaults to today.</param>
/// <param name="EndDate">Optional; must be after StartDate.</param>
[RequiredFields("serviceId", "roleTitle")]
public record CreateAssignmentRequest(int? ServiceId, string? RoleTitle, DateOnly? StartDate, DateOnly? EndDate);

/// <summary>Body of PATCH /assignments/{id}. Setting EndDate closes the assignment.</summary>
[RequiredFields("endDate")]
public record UpdateAssignmentRequest(DateOnly? EndDate);

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
