using Sohojatri.Api.Domain;

namespace Sohojatri.Api.Entities;

public class GroupMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public Guid SenderUserId { get; set; }
    public GroupMessageType MessageType { get; set; } = GroupMessageType.Chat;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    public RideGroup Group { get; set; } = null!;
    public User Sender { get; set; } = null!;
}
