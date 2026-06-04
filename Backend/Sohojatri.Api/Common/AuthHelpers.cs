using Microsoft.EntityFrameworkCore;
using Sohojatri.Api.Infrastructure;

namespace Sohojatri.Api.Common;

public static class AuthHelpers
{
    public static RouteGroupBuilder RequireSession(this RouteGroupBuilder group)
    {
        group.AddEndpointFilter(async (context, next) =>
        {
            var userId = context.HttpContext.GetCurrentUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            return await next(context);
        });

        return group;
    }

    public static Guid? GetCurrentUserId(this HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue("CurrentUserId", out var userIdObj) && userIdObj is Guid userId)
        {
            return userId;
        }

        return null;
    }

    public static async Task ResolveUserFromBearerTokenAsync(this HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
        {
            return;
        }

        var tokenValue = authorizationHeader.ToString();
        if (!tokenValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var token = tokenValue[7..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();
        var session = await dbContext.UserSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Token == token && x.ExpiresAt > DateTimeOffset.UtcNow);

        if (session is not null)
        {
            context.Items["CurrentUserId"] = session.UserId;
        }
    }
}
