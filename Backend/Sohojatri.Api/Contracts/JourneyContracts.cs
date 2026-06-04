using Sohojatri.Api.Domain;

namespace Sohojatri.Api.Contracts;

public record CreateJourneyRequest(
    double SourceLatitude,
    double SourceLongitude,
    double DestinationLatitude,
    double DestinationLongitude,
    DateTimeOffset DepartureTime,
    int PassengerCount,
    string? GenderPreference);

public record CircleRiderResponse(
    Guid JourneyId,
    Guid UserId,
    string Name,
    VerificationLevel VerificationLevel,
    double ReputationScore,
    int CompletedTrips,
    int CancelledTrips,
    DateTimeOffset DepartureTime,
    double SourceDistanceMeters,
    double DestinationDistanceMeters);

public record JourneyResultResponse(Guid JourneyId, JourneyStatus Status, Guid? GroupId, IReadOnlyList<CircleRiderResponse> SuggestedRiders);
