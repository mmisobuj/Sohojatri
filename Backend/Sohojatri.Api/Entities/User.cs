using Sohojatri.Api.Domain;

namespace Sohojatri.Api.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MobileNumber { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfilePhotoUrl { get; set; }
    public VerificationLevel VerificationLevel { get; set; } = VerificationLevel.MobileVerified;
    public double ReputationScore { get; set; } = 5.0;
    public int CompletedTrips { get; set; }
    public int CancelledTrips { get; set; }
    public bool IsAdmin { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<JourneyRequest> JourneyRequests { get; set; } = new List<JourneyRequest>();
}
