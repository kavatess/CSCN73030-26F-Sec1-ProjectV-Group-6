/**
 * @file StaffHrRoles.cs
 * @brief App roles (Entra ID) and the authorization policies built from them.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Auth;

/// <summary>
/// App roles defined on the Staff/HR app registration in Entra ID and granted to users, groups or
/// peer services. They arrive in the token's "roles" claim.
/// </summary>
public static class StaffHrRoles
{
    /// <summary>HR staff: create and change staff, assignments, credentials and requisitions; read the audit log.</summary>
    public const string Admin = "StaffHr.Admin";
    /// <summary>Read staff, departments, rosters, credentials, requisitions and eligibility (peer services).</summary>
    public const string Read = "StaffHr.Read";
    /// <summary>Department schedulers: add and remove roster entries.</summary>
    public const string RostersWrite = "StaffHr.Rosters.Write";
    /// <summary>Departments: raise hire and termination requisitions.</summary>
    public const string RequisitionsSubmit = "StaffHr.Requisitions.Submit";
    /// <summary>The Security service: sync security flags.</summary>
    public const string SecurityFlagsWrite = "StaffHr.SecurityFlags.Write";
    /// <summary>Payroll officers and the Budget service: the only role that sees salary.</summary>
    public const string Payroll = "StaffHr.Payroll";
    /// <summary>Auditors: read the audit log.</summary>
    public const string AuditRead = "StaffHr.Audit.Read";

    /// <summary>Every role, e.g. for the development sign-in.</summary>
    public static readonly string[] All = [Admin, Read, RostersWrite, RequisitionsSubmit, SecurityFlagsWrite, Payroll, AuditRead];
}

/// <summary>Authorization policy names used in [Authorize(Policy = ...)].</summary>
public static class StaffHrPolicies
{
    /// <summary>Any Staff/HR role: read non-sensitive data.</summary>
    public const string Read = "StaffHr.Policy.Read";
    /// <summary>Admin: change HR records and move requisitions through their workflow.</summary>
    public const string Manage = "StaffHr.Policy.Manage";
    /// <summary>Admin or Rosters.Write: add and remove shifts.</summary>
    public const string ManageRosters = "StaffHr.Policy.ManageRosters";
    /// <summary>Admin or Requisitions.Submit: raise requisitions.</summary>
    public const string SubmitRequisitions = "StaffHr.Policy.SubmitRequisitions";
    /// <summary>Admin or SecurityFlags.Write: read and sync security flags.</summary>
    public const string SecurityFlags = "StaffHr.Policy.SecurityFlags";
    /// <summary>Payroll only: salary (business rule 4). HR admins without the Payroll role cannot see pay.</summary>
    public const string Payroll = "StaffHr.Policy.Payroll";
    /// <summary>Admin or Audit.Read: read the audit log.</summary>
    public const string Audit = "StaffHr.Policy.Audit";
}
