using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Sohojatri.Api.Domain;
using Sohojatri.Api.Entities;
using Sohojatri.Api.Infrastructure;

namespace Sohojatri.Api.Features.Realtime;

public class CommuteHub(AppDbContext dbContext) : Hub
{
    public async Task JoinGroup(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
    }

    public async Task LeaveGroup(string groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
    }

    public async Task SendMessage(string groupId, Guid senderUserId, string content, GroupMessageType messageType = GroupMessageType.Chat)
    {
        if (!Guid.TryParse(groupId, out var parsedGroupId))
        {
            return;
        }

        var isMember = await dbContext.GroupMembers.AnyAsync(x => x.GroupId == parsedGroupId && x.UserId == senderUserId);
        if (!isMember)
        {
            return;
        }

        var message = new GroupMessage
        {
            GroupId = parsedGroupId,
            SenderUserId = senderUserId,
            Content = content,
            MessageType = messageType,
            SentAt = DateTimeOffset.UtcNow
        };

        dbContext.GroupMessages.Add(message);
        await dbContext.SaveChangesAsync();

        await Clients.Group(groupId).SendAsync("group_message", new
        {
            message.Id,
            message.GroupId,
            message.SenderUserId,
            message.Content,
            message.MessageType,
            message.SentAt
        });
    }

    public async Task ShareLocation(string groupId, Guid userId, double latitude, double longitude)
    {
        if (!Guid.TryParse(groupId, out var parsedGroupId))
        {
            return;
        }

        var isMember = await dbContext.GroupMembers.AnyAsync(x => x.GroupId == parsedGroupId && x.UserId == userId);
        if (!isMember)
        {
            return;
        }

        var location = await dbContext.GroupLiveLocations
            .FirstOrDefaultAsync(x => x.GroupId == parsedGroupId && x.UserId == userId);

        if (location is null)
        {
            dbContext.GroupLiveLocations.Add(new GroupLiveLocation
            {
                GroupId = parsedGroupId,
                UserId = userId,
                Latitude = latitude,
                Longitude = longitude,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
        else
        {
            location.Latitude = latitude;
            location.Longitude = longitude;
            location.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync();

        await Clients.Group(groupId).SendAsync("location_update", new
        {
            GroupId = parsedGroupId,
            UserId = userId,
            Latitude = latitude,
            Longitude = longitude,
            UpdatedAt = DateTimeOffset.UtcNow
        });
    }
}
