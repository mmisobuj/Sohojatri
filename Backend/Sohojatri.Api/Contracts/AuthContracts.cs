namespace Sohojatri.Api.Contracts;

public record RequestOtpRequest(string MobileNumber);
public record VerifyOtpRequest(string MobileNumber, string Code, string? DisplayName);

public record AuthResponse(Guid UserId, string DisplayName, string MobileNumber, string Token, DateTimeOffset ExpiresAt);
