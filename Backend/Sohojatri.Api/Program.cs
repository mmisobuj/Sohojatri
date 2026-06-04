using System.Text.Json.Serialization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Sohojatri.Api.Common;
using Sohojatri.Api.Contracts;
using Sohojatri.Api.Domain;
using Sohojatri.Api.Entities;
using Sohojatri.Api.Features.Auth;
using Sohojatri.Api.Features.Matching;
using Sohojatri.Api.Features.Realtime;
using Sohojatri.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"];

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.Configure<MatchingOptions>(builder.Configuration.GetSection(MatchingOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "sohojatri";
});

builder.Services.AddScoped<IMatchingService, MatchingService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddHostedService<GroupLifecycleService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    await context.ResolveUserFromBearerTokenAsync();
    await next();
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/auth/request-otp", async (RequestOtpRequest request, IOtpService otpService, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.MobileNumber))
    {
        return Results.BadRequest(new { error = "Mobile number is required" });
    }

    await otpService.IssueCodeAsync(request.MobileNumber, cancellationToken);
    return Results.Ok(new { message = "OTP issued", expiresInSeconds = 180 });
});

app.MapPost("/api/auth/verify-otp", async (VerifyOtpRequest request, IOtpService otpService, AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var isValid = await otpService.VerifyCodeAsync(request.MobileNumber, request.Code, cancellationToken);
    if (!isValid)
    {
        return Results.BadRequest(new { error = "Invalid OTP" });
    }

    var user = await dbContext.Users.FirstOrDefaultAsync(x => x.MobileNumber == request.MobileNumber);
    if (user is null)
    {
        var generatedId = Guid.NewGuid();
        user = new User
        {
            Id = generatedId,
            MobileNumber = request.MobileNumber,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? $"Rider_{generatedId.ToString("N")[..8]}" : request.DisplayName.Trim(),
            VerificationLevel = VerificationLevel.MobileVerified
        };

        dbContext.Users.Add(user);
    }
    else if (!string.IsNullOrWhiteSpace(request.DisplayName))
    {
        user.DisplayName = request.DisplayName.Trim();
    }

    var tokenBytes = RandomNumberGenerator.GetBytes(32);
    var token = Convert.ToBase64String(tokenBytes);
    var session = new UserSession
    {
        UserId = user.Id,
        Token = token,
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
    };

    dbContext.UserSessions.Add(session);
    await dbContext.SaveChangesAsync();

    return Results.Ok(new AuthResponse(user.Id, user.DisplayName, user.MobileNumber, token, session.ExpiresAt));
});

var profile = app.MapGroup("/api/profile").RequireSession();

profile.MapGet("/me", async (HttpContext context, AppDbContext dbContext) =>
{
    var userId = context.GetCurrentUserId()!.Value;
    var user = await dbContext.Users.FindAsync(userId);

    return user is null
        ? Results.NotFound()
        : Results.Ok(new ProfileResponse(
            user.Id,
            user.DisplayName,
            user.MobileNumber,
            user.ProfilePhotoUrl,
            user.VerificationLevel,
            user.ReputationScore,
            user.CompletedTrips,
            user.CancelledTrips));
});

profile.MapPut("/me", async (HttpContext context, UpdateProfileRequest request, AppDbContext dbContext) =>
{
    var userId = context.GetCurrentUserId()!.Value;
    var user = await dbContext.Users.FindAsync(userId);
    if (user is null)
    {
        return Results.NotFound();
    }

    user.DisplayName = request.DisplayName;
    user.ProfilePhotoUrl = request.ProfilePhotoUrl;

    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});

var journeys = app.MapGroup("/api/journeys").RequireSession();

journeys.MapPost("/", async (HttpContext context, CreateJourneyRequest request, IMatchingService matchingService) =>
{
    var userId = context.GetCurrentUserId()!.Value;
    var result = await matchingService.CreateJourneyAndMatchAsync(userId, request);
    return Results.Ok(result);
});

journeys.MapGet("/instant-circle", async (
    HttpContext context,
    double sourceLat,
    double sourceLon,
    double destinationLat,
    double destinationLon,
    DateTimeOffset departureTime,
    AppDbContext dbContext,
    IMatchingService matchingService) =>
{
    var userId = context.GetCurrentUserId()!.Value;
    var probe = new JourneyRequest
    {
        UserId = userId,
        SourceLatitude = sourceLat,
        SourceLongitude = sourceLon,
        DestinationLatitude = destinationLat,
        DestinationLongitude = destinationLon,
        DepartureTime = departureTime,
        Status = JourneyStatus.Pending,
        PassengerCount = 1
    };

    var riders = await matchingService.GetInstantCommuteCircleAsync(probe, userId);
    return Results.Ok(new { membersFound = riders.Count, riders });
});

journeys.MapPost("/{journeyId:guid}/cancel", async (HttpContext context, Guid journeyId, AppDbContext dbContext) =>
{
    var userId = context.GetCurrentUserId()!.Value;
    var journey = await dbContext.JourneyRequests.FirstOrDefaultAsync(x => x.Id == journeyId && x.UserId == userId);
    if (journey is null)
    {
        return Results.NotFound();
    }

    journey.Status = JourneyStatus.Cancelled;

    var user = await dbContext.Users.FindAsync(userId);
    if (user is not null)
    {
        user.CancelledTrips += 1;
    }

    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});

var groups = app.MapGroup("/api/groups").RequireSession();

groups.MapGet("/{groupId:guid}", async (Guid groupId, AppDbContext dbContext) =>
{
    var group = await dbContext.RideGroups
        .Include(x => x.Members).ThenInclude(x => x.User)
        .FirstOrDefaultAsync(x => x.Id == groupId);

    if (group is null)
    {
        return Results.NotFound();
    }

    var messages = await dbContext.GroupMessages
        .Where(x => x.GroupId == groupId)
        .OrderByDescending(x => x.SentAt)
        .Take(50)
        .OrderBy(x => x.SentAt)
        .Select(x => new { x.Id, x.SenderUserId, x.Content, x.MessageType, x.SentAt })
        .ToListAsync();

    return Results.Ok(new
    {
        group.Id,
        group.CircleName,
        group.Status,
        group.ExpiresAt,
        group.EstimatedFarePerPerson,
        members = group.Members.Select(x => new GroupMemberResponse(x.UserId, x.User.DisplayName, x.User.VerificationLevel, x.User.ReputationScore, x.User.CompletedTrips)),
        messages
    });
});

groups.MapPost("/{groupId:guid}/messages", async (HttpContext context, Guid groupId, GroupMessageRequest request, AppDbContext dbContext) =>
{
    var userId = context.GetCurrentUserId()!.Value;
    var isMember = await dbContext.GroupMembers.AnyAsync(x => x.GroupId == groupId && x.UserId == userId);
    if (!isMember)
    {
        return Results.Forbid();
    }

    var message = new GroupMessage
    {
        GroupId = groupId,
        SenderUserId = userId,
        Content = request.Content,
        MessageType = request.MessageType
    };

    dbContext.GroupMessages.Add(message);
    await dbContext.SaveChangesAsync();

    return Results.Ok(new { message.Id, message.SentAt });
});

groups.MapPost("/{groupId:guid}/location", async (HttpContext context, Guid groupId, LocationUpdateRequest request, AppDbContext dbContext) =>
{
    var userId = context.GetCurrentUserId()!.Value;

    var location = await dbContext.GroupLiveLocations.FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserId == userId);
    if (location is null)
    {
        dbContext.GroupLiveLocations.Add(new GroupLiveLocation
        {
            GroupId = groupId,
            UserId = userId,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        });
    }
    else
    {
        location.Latitude = request.Latitude;
        location.Longitude = request.Longitude;
        location.UpdatedAt = DateTimeOffset.UtcNow;
    }

    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});

groups.MapPost("/{groupId:guid}/start-trip", async (HttpContext context, Guid groupId, StartTripRequest request, AppDbContext dbContext) =>
{
    var userId = context.GetCurrentUserId()!.Value;
    var membership = await dbContext.GroupMembers.FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserId == userId);
    if (membership is null)
    {
        return Results.Forbid();
    }

    var trip = await dbContext.Trips.FirstOrDefaultAsync(x => x.GroupId == groupId);
    if (trip is null)
    {
        trip = new Trip
        {
            GroupId = groupId,
            Status = TripStatus.Started,
            StartedAt = DateTimeOffset.UtcNow
        };
        dbContext.Trips.Add(trip);
    }
    else
    {
        trip.Status = TripStatus.Started;
        trip.StartedAt ??= DateTimeOffset.UtcNow;
    }

    var group = await dbContext.RideGroups.FindAsync(groupId);
    if (group is not null && request.EstimatedFarePerPerson is not null)
    {
        group.EstimatedFarePerPerson = request.EstimatedFarePerPerson;
    }

    await dbContext.SaveChangesAsync();
    return Results.Ok(new { tripId = trip.Id, status = trip.Status });
});

groups.MapPost("/{groupId:guid}/complete-trip", async (HttpContext context, Guid groupId, AppDbContext dbContext) =>
{
    var userId = context.GetCurrentUserId()!.Value;
    var membership = await dbContext.GroupMembers.FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserId == userId);
    if (membership is null)
    {
        return Results.Forbid();
    }

    var trip = await dbContext.Trips.FirstOrDefaultAsync(x => x.GroupId == groupId);
    if (trip is null)
    {
        return Results.NotFound(new { error = "Trip not started" });
    }

    trip.Status = TripStatus.Completed;
    trip.CompletedAt = DateTimeOffset.UtcNow;

    var journeys = await dbContext.JourneyRequests.Where(x => x.GroupId == groupId && x.Status == JourneyStatus.Matched).ToListAsync();
    foreach (var journey in journeys)
    {
        journey.Status = JourneyStatus.Completed;
    }

    var userIds = await dbContext.GroupMembers.Where(x => x.GroupId == groupId).Select(x => x.UserId).ToListAsync();
    var users = await dbContext.Users.Where(x => userIds.Contains(x.Id)).ToListAsync();
    foreach (var user in users)
    {
        user.CompletedTrips += 1;
    }

    await dbContext.SaveChangesAsync();
    return Results.Ok(new { tripId = trip.Id, trip.Status, trip.CompletedAt });
});

groups.MapPost("/{groupId:guid}/ratings", async (HttpContext context, Guid groupId, SubmitRatingRequest request, AppDbContext dbContext) =>
{
    var userId = context.GetCurrentUserId()!.Value;
    var trip = await dbContext.Trips.FirstOrDefaultAsync(x => x.GroupId == groupId && x.Status == TripStatus.Completed);
    if (trip is null)
    {
        return Results.BadRequest(new { error = "Completed trip required" });
    }

    var isMember = await dbContext.GroupMembers.AnyAsync(x => x.GroupId == groupId && x.UserId == userId);
    if (!isMember)
    {
        return Results.Forbid();
    }

    if (request.Stars is < 1 or > 5)
    {
        return Results.BadRequest(new { error = "Stars must be between 1 and 5" });
    }

    var rating = new Rating
    {
        TripId = trip.Id,
        RaterUserId = userId,
        RatedUserId = request.RatedUserId,
        Stars = request.Stars,
        Comment = request.Comment
    };

    dbContext.Ratings.Add(rating);
    await dbContext.SaveChangesAsync();

    var ratedUser = await dbContext.Users.FindAsync(request.RatedUserId);
    if (ratedUser is not null)
    {
        var avg = await dbContext.Ratings.Where(x => x.RatedUserId == request.RatedUserId).AverageAsync(x => (double)x.Stars);
        ratedUser.ReputationScore = Math.Round(avg, 2);
        await dbContext.SaveChangesAsync();
    }

    return Results.Ok(new { rating.Id });
});

var safety = app.MapGroup("/api/safety").RequireSession();

safety.MapPost("/reports", async (HttpContext context, ReportUserRequest request, AppDbContext dbContext) =>
{
    var reporterId = context.GetCurrentUserId()!.Value;

    var report = new UserReport
    {
        ReporterUserId = reporterId,
        ReportedUserId = request.ReportedUserId,
        Reason = request.Reason,
        Notes = request.Notes,
        Status = ModerationStatus.Open
    };

    dbContext.UserReports.Add(report);
    await dbContext.SaveChangesAsync();

    return Results.Ok(new { report.Id, report.Status });
});

safety.MapPost("/block/{blockedUserId:guid}", async (HttpContext context, Guid blockedUserId, AppDbContext dbContext) =>
{
    var userId = context.GetCurrentUserId()!.Value;

    var existing = await dbContext.UserBlocks.FirstOrDefaultAsync(x => x.UserId == userId && x.BlockedUserId == blockedUserId);
    if (existing is not null)
    {
        return Results.NoContent();
    }

    dbContext.UserBlocks.Add(new UserBlock
    {
        UserId = userId,
        BlockedUserId = blockedUserId
    });

    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});

var admin = app.MapGroup("/api/admin").RequireSession();
admin.MapGet("/moderation/reports", async (HttpContext context, AppDbContext dbContext, ModerationStatus? status) =>
{
    var userId = context.GetCurrentUserId()!.Value;
    var isAdmin = await dbContext.Users.Where(x => x.Id == userId).Select(x => x.IsAdmin).FirstOrDefaultAsync();
    if (!isAdmin)
    {
        return Results.Forbid();
    }

    var query = dbContext.UserReports.AsNoTracking();
    if (status is not null)
    {
        query = query.Where(x => x.Status == status.Value);
    }

    var reports = await query
        .OrderByDescending(x => x.CreatedAt)
        .Take(200)
        .Select(x => new
        {
            x.Id,
            x.ReporterUserId,
            x.ReportedUserId,
            x.Reason,
            x.Status,
            x.Notes,
            x.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(reports);
});

app.MapGet("/api/metrics/overview", async (HttpContext context, AppDbContext dbContext) =>
{
    var userId = context.GetCurrentUserId();
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var isAdmin = await dbContext.Users.Where(x => x.Id == userId.Value).Select(x => x.IsAdmin).FirstOrDefaultAsync();
    if (!isAdmin)
    {
        return Results.Forbid();
    }

    var totalJourneys = await dbContext.JourneyRequests.CountAsync();
    var completedTrips = await dbContext.Trips.CountAsync(x => x.Status == TripStatus.Completed);
    var cancelledJourneys = await dbContext.JourneyRequests.CountAsync(x => x.Status == JourneyStatus.Cancelled);
    var activeCircles = await dbContext.RideGroups.CountAsync(x => x.Status == GroupStatus.Active);
    var matchedWindows = await dbContext.JourneyRequests
        .Where(x => (x.Status == JourneyStatus.Matched || x.Status == JourneyStatus.Completed) && x.MatchedAt != null)
        .Select(x => new { x.CreatedAt, x.MatchedAt })
        .ToListAsync();

    var avgMatchResponseSeconds = matchedWindows.Count == 0
        ? 0
        : matchedWindows.Average(x => (x.MatchedAt!.Value - x.CreatedAt).TotalSeconds);

    var matchSuccessRate = totalJourneys == 0 ? 0 : Math.Round((double)await dbContext.JourneyRequests.CountAsync(x => x.Status == JourneyStatus.Matched || x.Status == JourneyStatus.Completed) * 100 / totalJourneys, 2);
    var cancellationRate = totalJourneys == 0 ? 0 : Math.Round((double)cancelledJourneys * 100 / totalJourneys, 2);

    return Results.Ok(new
    {
        matchSuccessRate,
        cancellationRate,
        avgMatchResponseSeconds,
        activeCircles,
        completedTrips,
        generatedAt = DateTimeOffset.UtcNow
    });
});

app.MapHub<CommuteHub>("/hubs/commute");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.Run();
