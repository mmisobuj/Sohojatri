using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sohojatri.Api.Contracts;
using Sohojatri.Api.Domain;
using Sohojatri.Api.Entities;
using Sohojatri.Api.Infrastructure;

namespace Sohojatri.Api.Features.Matching;

public interface IMatchingService
{
    Task<IReadOnlyList<CircleRiderResponse>> GetInstantCommuteCircleAsync(JourneyRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<JourneyResultResponse> CreateJourneyAndMatchAsync(Guid currentUserId, CreateJourneyRequest request, CancellationToken cancellationToken = default);
}

public class MatchingService(AppDbContext dbContext, IOptions<MatchingOptions> options) : IMatchingService
{
    private readonly MatchingOptions _options = options.Value;

    public async Task<JourneyResultResponse> CreateJourneyAndMatchAsync(Guid currentUserId, CreateJourneyRequest request, CancellationToken cancellationToken = default)
    {
        var journey = new JourneyRequest
        {
            UserId = currentUserId,
            SourceLatitude = request.SourceLatitude,
            SourceLongitude = request.SourceLongitude,
            DestinationLatitude = request.DestinationLatitude,
            DestinationLongitude = request.DestinationLongitude,
            DepartureTime = request.DepartureTime,
            PassengerCount = request.PassengerCount,
            GenderPreference = request.GenderPreference,
            Status = JourneyStatus.Pending
        };

        dbContext.JourneyRequests.Add(journey);
        await dbContext.SaveChangesAsync(cancellationToken);

        var suggestions = await GetInstantCommuteCircleAsync(journey, currentUserId, cancellationToken);

        if (suggestions.Count < 1)
        {
            return new JourneyResultResponse(journey.Id, JourneyStatus.Pending, null, suggestions);
        }

        var maxMatches = Math.Min(Math.Max(_options.MaxGroupSize - 1, 0), suggestions.Count);
        var selectedIds = suggestions.Take(maxMatches).Select(x => x.JourneyId).ToArray();
        var matchedJourneys = await dbContext.JourneyRequests
            .Where(x => selectedIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var group = new RideGroup
        {
            CircleName = $"{DateTimeOffset.UtcNow:HHmm}-Commute-Circle",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.GroupLifetimeMinutes),
            Status = GroupStatus.Active
        };

        dbContext.RideGroups.Add(group);
        await dbContext.SaveChangesAsync(cancellationToken);

        var memberIds = matchedJourneys.Select(x => x.UserId).Append(currentUserId).Distinct().ToArray();
        var members = memberIds.Select((uid, idx) => new GroupMember
        {
            GroupId = group.Id,
            UserId = uid,
            IsHost = idx == 0
        });

        dbContext.GroupMembers.AddRange(members);

        journey.Status = JourneyStatus.Matched;
        journey.MatchedAt = DateTimeOffset.UtcNow;
        journey.GroupId = group.Id;

        foreach (var matched in matchedJourneys)
        {
            matched.Status = JourneyStatus.Matched;
            matched.MatchedAt = DateTimeOffset.UtcNow;
            matched.GroupId = group.Id;
        }

        dbContext.GroupMessages.Add(new GroupMessage
        {
            GroupId = group.Id,
            SenderUserId = currentUserId,
            MessageType = GroupMessageType.System,
            Content = "Instant Commute Circle created. Please discuss vehicle and fare."
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new JourneyResultResponse(journey.Id, journey.Status, group.Id, suggestions);
    }

    public async Task<IReadOnlyList<CircleRiderResponse>> GetInstantCommuteCircleAsync(JourneyRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var windowStart = request.DepartureTime.AddMinutes(-_options.TimeWindowMinutes);
        var windowEnd = request.DepartureTime.AddMinutes(_options.TimeWindowMinutes);

        var blockedByCurrent = await dbContext.UserBlocks
            .Where(x => x.UserId == currentUserId)
            .Select(x => x.BlockedUserId)
            .ToListAsync(cancellationToken);

        var blockedCurrent = await dbContext.UserBlocks
            .Where(x => x.BlockedUserId == currentUserId)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        var candidates = await dbContext.JourneyRequests
            .Where(x => x.Status == JourneyStatus.Pending
                        && x.UserId != currentUserId
                        && x.DepartureTime >= windowStart
                        && x.DepartureTime <= windowEnd
                        && !blockedByCurrent.Contains(x.UserId)
                        && !blockedCurrent.Contains(x.UserId))
            .Include(x => x.User)
            .ToListAsync(cancellationToken);

        return candidates
            .Select(c => new
            {
                Journey = c,
                SourceDistance = DistanceMeters(request.SourceLatitude, request.SourceLongitude, c.SourceLatitude, c.SourceLongitude),
                DestinationDistance = DistanceMeters(request.DestinationLatitude, request.DestinationLongitude, c.DestinationLatitude, c.DestinationLongitude)
            })
            .Where(x => x.SourceDistance <= _options.CurrentLocationRadiusMeters
                        && x.DestinationDistance <= _options.DestinationRadiusMeters)
            .OrderBy(x => x.DestinationDistance)
            .ThenBy(x => x.SourceDistance)
            .Select(x => new CircleRiderResponse(
                x.Journey.Id,
                x.Journey.UserId,
                x.Journey.User.DisplayName,
                x.Journey.User.VerificationLevel,
                x.Journey.User.ReputationScore,
                x.Journey.User.CompletedTrips,
                x.Journey.User.CancelledTrips,
                x.Journey.DepartureTime,
                Math.Round(x.SourceDistance, 2),
                Math.Round(x.DestinationDistance, 2)))
            .Take(_options.MaxGroupSize - 1)
            .ToList();
    }

    private static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6371000;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusMeters * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
