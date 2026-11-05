namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

/// <summary>A department in GET /departments.</summary>
public record DepartmentDto(string Code, string Name, int ServiceCount);

/// <summary>A service in GET /departments/{code}/services. ServiceId is what other endpoints filter on.</summary>
public record ServiceDto(int ServiceId, string Name, string DepartmentCode);

/// <summary>Per-department numbers for the Workforce overview.</summary>
/// <param name="Active">Staff whose home department this is and whose status is Active or Probation.</param>
/// <param name="OnDuty">Distinct staff on a non-cancelled shift right now in this department's services.</param>
/// <param name="Vacancies">Unfilled positions on Approved or Filling hire requisitions.</param>
public record DepartmentHeadcountDto(string Code, string Name, int Active, int OnDuty, int Vacancies);
