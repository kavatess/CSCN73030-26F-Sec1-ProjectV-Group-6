/**
 * @file ICurrentUser.cs
 * @brief Who is making the current request, for audit records and authorization.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application;

/// <summary>The caller of the current request.</summary>
public interface ICurrentUser
{
    /// <summary>
    /// Identifier written to AuditLog.ActorId: the signed-in user or service, "anonymous" for an
    /// unauthenticated request, or "system" outside a request (e.g. the seeder).
    /// </summary>
    string ActorId { get; }
}
