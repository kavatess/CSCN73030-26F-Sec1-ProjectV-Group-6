/**
 * @file IntegrationEvents.cs
 * @brief Payloads of the integration events Staff/HR publishes to other CMES services.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Events;

/// <summary>Event names and the current payload schema version.</summary>
public static class IntegrationEventTypes
{
    /// <summary>A new employee was onboarded.</summary>
    public const string StaffOnboarded = "Staff.Onboarded";

    /// <summary>An employee was terminated and is no longer eligible for duty.</summary>
    public const string StaffTerminated = "Staff.Terminated";

    /// <summary>Schema version of the payloads below. Bump it when a payload changes shape.</summary>
    public const int SchemaVersion = 1;
}

/// <summary>
/// Payload of Staff.Onboarded. Deliberately minimal: no date of birth, citizen id or salary.
/// </summary>
/// <param name="StaffNumber">STF-0000000. Peer services store it as their foreign key.</param>
/// <param name="FirstName">Legal first name.</param>
/// <param name="LastName">Legal last name.</param>
/// <param name="DisplayName">Preferred (or first) name plus last name.</param>
/// <param name="Email">Work email.</param>
/// <param name="DepartmentCode">Home department code, e.g. EMS.</param>
/// <param name="EmploymentType">FullTime, PartTime, Contract or Seasonal.</param>
/// <param name="EmploymentStatus">Active or Probation.</param>
/// <param name="HireDate">First day of employment.</param>
/// <param name="ServiceId">Service of the initial assignment, if one was given.</param>
/// <param name="RoleTitle">Role of the initial assignment, if one was given.</param>
public record StaffOnboardedEvent(
    string StaffNumber,
    string FirstName,
    string LastName,
    string DisplayName,
    string Email,
    string DepartmentCode,
    string EmploymentType,
    string EmploymentStatus,
    DateOnly HireDate,
    int? ServiceId,
    string? RoleTitle);

/// <summary>Payload of Staff.Terminated.</summary>
/// <param name="StaffNumber">STF-0000000 of the terminated employee. The number is never reused.</param>
/// <param name="DepartmentCode">Home department code at termination.</param>
/// <param name="EffectiveDate">Last day of employment.</param>
/// <param name="RequisitionNumber">The termination requisition that authorised it.</param>
/// <param name="CancelledShifts">How many future roster entries were cancelled.</param>
public record StaffTerminatedEvent(
    string StaffNumber,
    string DepartmentCode,
    DateOnly EffectiveDate,
    string RequisitionNumber,
    int CancelledShifts);
