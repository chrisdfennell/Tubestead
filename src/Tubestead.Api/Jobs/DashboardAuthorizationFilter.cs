using Hangfire.Dashboard;
using Tubestead.Domain;

namespace Tubestead.Api.Jobs;

/// <summary>Restricts the Hangfire dashboard to signed-in admins.</summary>
public class AdminDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var user = context.GetHttpContext().User;
        return user.Identity?.IsAuthenticated == true && user.IsInRole(Roles.Admin);
    }
}
