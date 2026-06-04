using Sohojatri.Api.Domain;

namespace Sohojatri.Api.Contracts;

public record ReportUserRequest(Guid ReportedUserId, ReportReason Reason, string? Notes);
