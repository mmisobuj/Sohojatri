namespace Sohojatri.Api.Entities;

public class Rating
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Guid RaterUserId { get; set; }
    public Guid RatedUserId { get; set; }
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Trip Trip { get; set; } = null!;
}
