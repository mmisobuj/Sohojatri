using Microsoft.EntityFrameworkCore;
using Sohojatri.Api.Domain;
using Sohojatri.Api.Infrastructure;

namespace Sohojatri.Api.Features.Realtime;

public class GroupLifecycleService(IServiceScopeFactory scopeFactory, ILogger<GroupLifecycleService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var now = DateTimeOffset.UtcNow;
                var expired = await dbContext.RideGroups
                    .Where(x => x.Status == GroupStatus.Active && x.ExpiresAt <= now)
                    .ToListAsync(stoppingToken);

                if (expired.Count > 0)
                {
                    foreach (var group in expired)
                    {
                        group.Status = GroupStatus.Archived;
                    }

                    var expiredIds = expired.Select(x => x.Id).ToArray();
                    var journeys = await dbContext.JourneyRequests
                        .Where(x => x.GroupId.HasValue && expiredIds.Contains(x.GroupId.Value) && x.Status == JourneyStatus.Matched)
                        .ToListAsync(stoppingToken);

                    foreach (var journey in journeys)
                    {
                        journey.Status = JourneyStatus.Expired;
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("Archived {Count} expired commute circles", expired.Count);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to archive expired groups");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
