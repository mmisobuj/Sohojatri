using Sohojatri.Api.Domain;

namespace Sohojatri.Api.Entities;

public class UserReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReporterUserId { get; set; }
    public Guid ReportedUserId { get; set; }
    public ReportReason Reason { get; set; }
    public string? Notes { get; set; }
    public ModerationStatus Status { get; set; } = ModerationStatus.Open;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
