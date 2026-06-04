using Sohojatri.Api.Domain;

namespace Sohojatri.Api.Entities;

public class RideGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CircleName { get; set; } = string.Empty;
    public GroupStatus Status { get; set; } = GroupStatus.Active;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public decimal? EstimatedFarePerPerson { get; set; }

    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    public ICollection<GroupMessage> Messages { get; set; } = new List<GroupMessage>();
}
