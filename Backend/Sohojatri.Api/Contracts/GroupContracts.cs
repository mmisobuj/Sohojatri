using Sohojatri.Api.Domain;

namespace Sohojatri.Api.Contracts;

public record GroupMemberResponse(Guid UserId, string Name, VerificationLevel VerificationLevel, double ReputationScore, int CompletedTrips);
public record GroupMessageRequest(string Content, GroupMessageType MessageType);
public record LocationUpdateRequest(double Latitude, double Longitude);
public record StartTripRequest(decimal? EstimatedFarePerPerson);
public record SubmitRatingRequest(Guid RatedUserId, int Stars, string? Comment);
