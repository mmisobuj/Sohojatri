using Microsoft.EntityFrameworkCore;
using Sohojatri.Api.Domain;
using Sohojatri.Api.Entities;

namespace Sohojatri.Api.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<JourneyRequest> JourneyRequests => Set<JourneyRequest>();
    public DbSet<RideGroup> RideGroups => Set<RideGroup>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<GroupMessage> GroupMessages => Set<GroupMessage>();
    public DbSet<GroupLiveLocation> GroupLiveLocations => Set<GroupLiveLocation>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<UserReport> UserReports => Set<UserReport>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(x => x.MobileNumber).IsUnique();
        modelBuilder.Entity<UserSession>().HasIndex(x => x.Token).IsUnique();
        modelBuilder.Entity<UserSession>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId);

        modelBuilder.Entity<JourneyRequest>()
            .HasOne(x => x.User)
            .WithMany(x => x.JourneyRequests)
            .HasForeignKey(x => x.UserId);

        modelBuilder.Entity<JourneyRequest>()
            .HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<GroupMember>()
            .HasOne(x => x.Group)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.GroupId);

        modelBuilder.Entity<GroupMember>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId);

        modelBuilder.Entity<GroupMember>()
            .HasIndex(x => new { x.GroupId, x.UserId })
            .IsUnique();

        modelBuilder.Entity<GroupMessage>()
            .HasOne(x => x.Group)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.GroupId);

        modelBuilder.Entity<GroupMessage>()
            .HasOne(x => x.Sender)
            .WithMany()
            .HasForeignKey(x => x.SenderUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Trip>()
            .HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId);

        modelBuilder.Entity<UserBlock>()
            .HasIndex(x => new { x.UserId, x.BlockedUserId })
            .IsUnique();

        modelBuilder.Entity<JourneyRequest>()
            .Property(x => x.Status)
            .HasConversion<string>();
        modelBuilder.Entity<RideGroup>()
            .Property(x => x.Status)
            .HasConversion<string>();
        modelBuilder.Entity<Trip>()
            .Property(x => x.Status)
            .HasConversion<string>();
        modelBuilder.Entity<User>()
            .Property(x => x.VerificationLevel)
            .HasConversion<string>();
        modelBuilder.Entity<UserReport>()
            .Property(x => x.Reason)
            .HasConversion<string>();
        modelBuilder.Entity<UserReport>()
            .Property(x => x.Status)
            .HasConversion<string>();
        modelBuilder.Entity<GroupMessage>()
            .Property(x => x.MessageType)
            .HasConversion<string>();

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var pilot1 = new User
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            MobileNumber = "8801700000001",
            DisplayName = "Sobuj",
            VerificationLevel = VerificationLevel.FrequentTraveller,
            ReputationScore = 4.8,
            CompletedTrips = 120,
            CancelledTrips = 2,
            IsAdmin = true,
            CreatedAt = now
        };

        var pilot2 = new User
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            MobileNumber = "8801700000002",
            DisplayName = "Nabila",
            VerificationLevel = VerificationLevel.MobileVerified,
            ReputationScore = 4.6,
            CompletedTrips = 55,
            CancelledTrips = 3,
            IsAdmin = false,
            CreatedAt = now
        };

        modelBuilder.Entity<User>().HasData(pilot1, pilot2);
    }
}
