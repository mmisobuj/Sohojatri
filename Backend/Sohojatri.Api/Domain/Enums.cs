namespace Sohojatri.Api.Domain;

public enum VerificationLevel
{
    MobileVerified = 1,
    NIDVerified = 2,
    FrequentTraveller = 3
}

public enum JourneyStatus
{
    Pending = 1,
    Matched = 2,
    Cancelled = 3,
    Completed = 4,
    Expired = 5
}

public enum GroupStatus
{
    Active = 1,
    Archived = 2,
    Cancelled = 3
}

public enum TripStatus
{
    Planned = 1,
    Started = 2,
    Completed = 3,
    Cancelled = 4
}

public enum ReportReason
{
    FakeUser = 1,
    Harassment = 2,
    Spam = 3,
    UnsafeBehavior = 4
}

public enum ModerationStatus
{
    Open = 1,
    UnderReview = 2,
    Resolved = 3,
    Dismissed = 4
}

public enum GroupMessageType
{
    Chat = 1,
    FareProposal = 2,
    System = 3
}
