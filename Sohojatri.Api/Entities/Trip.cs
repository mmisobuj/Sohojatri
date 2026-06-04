using Sohojatri.Api.Domain;

namespace Sohojatri.Api.Entities;

public class Trip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public TripStatus Status { get; set; } = TripStatus.Planned;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public RideGroup Group { get; set; } = null!;
}
