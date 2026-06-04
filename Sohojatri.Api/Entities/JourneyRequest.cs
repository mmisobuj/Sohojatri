using Sohojatri.Api.Domain;

namespace Sohojatri.Api.Entities;

public class JourneyRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public double SourceLatitude { get; set; }
    public double SourceLongitude { get; set; }
    public double DestinationLatitude { get; set; }
    public double DestinationLongitude { get; set; }
    public DateTimeOffset DepartureTime { get; set; }
    public int PassengerCount { get; set; }
    public string? GenderPreference { get; set; }
    public JourneyStatus Status { get; set; } = JourneyStatus.Pending;
    public DateTimeOffset? MatchedAt { get; set; }
    public Guid? GroupId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;
    public RideGroup? Group { get; set; }
}
