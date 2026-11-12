/**
 * @file HttpCurrentUser.cs
 * @brief ICurrentUser implementation that reads the caller from the current HTTP request.
 */

using System.Security.Claims;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;

/// <summary>Reads the caller from <see cref="HttpContext.User"/>.</summary>
public class HttpCurrentUser : ICurrentUser
{
    /// <summary>ActorId used when there is no HTTP request, e.g. while seeding.</summary>
    public const string SystemActor = "system";

    /// <summary>ActorId used for a request without a signed-in caller.</summary>
    public const string AnonymousActor = "anonymous";

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Creates the accessor.</summary>
    /// <param name="httpContextAccessor">Gives access to the current request, if any.</param>
    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// The caller's preferred user name, else its object id, else its name claim. Falls back to
    /// "anonymous" inside a request and "system" outside one.
    /// </summary>
    public string ActorId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return SystemActor;

            var user = context.User;
            if (user.Identity?.IsAuthenticated != true) return AnonymousActor;

            return user.FindFirstValue("preferred_username")
                   ?? user.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
                   ?? user.FindFirstValue("oid")
                   ?? user.Identity.Name
                   ?? AnonymousActor;
        }
    }
}
