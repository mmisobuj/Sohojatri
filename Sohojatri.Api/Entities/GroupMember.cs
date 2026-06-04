namespace Sohojatri.Api.Entities;

public class GroupMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public bool IsHost { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public RideGroup Group { get; set; } = null!;
    public User User { get; set; } = null!;
}
