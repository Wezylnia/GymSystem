using Microsoft.AspNetCore.Authorization;

namespace GymSystem.Mvc.Authorization;

public class GymOwnerRequirement : IAuthorizationRequirement
{
}

public class GymOwnerAuthorizationHandler : AuthorizationHandler<GymOwnerRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GymOwnerAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        GymOwnerRequirement requirement)
    {
        // Admin her zaman başarılı
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // GymOwner için GymLocationId kontrolü
        if (context.User.IsInRole("GymOwner"))
        {
            var gymLocationIdClaim = context.User.FindFirst("GymLocationId");
            if (gymLocationIdClaim != null)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
