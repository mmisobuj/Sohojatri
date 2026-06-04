namespace Sohojatri.Api.Contracts;

public record ProfileResponse(
    Guid UserId,
    string DisplayName,
    string MobileNumber,
    string? ProfilePhotoUrl,
    Sohojatri.Api.Domain.VerificationLevel VerificationLevel,
    double ReputationScore,
    int CompletedTrips,
    int CancelledTrips);

public record UpdateProfileRequest(string DisplayName, string? ProfilePhotoUrl);
