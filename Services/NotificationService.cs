using MyApi.Models;
using MyApi.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using Microsoft.AspNetCore.Identity;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace MyApi.Services;

public class NotificationService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private async Task CreateNotification(string recipientId, string senderId, string message, NotificationType type, string? url = null)
    {
        var notification = new Notification
        {
            RecipientId = recipientId,
            SenderId = senderId,
            Message = message,
            Type = type,
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

    }
    public async Task SendNotification(string recipientId, string senderId, string message, NotificationType type, string? url = null)
    {
        var notification = new Notification
        {
            RecipientId = recipientId,
            SenderId = senderId,
            Message = message,
            Url = url,
            Type = type,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }
    public async Task<bool> MarkasRead(int id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null) return false;
        notification.IsRead = true;
        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<List<NotificationSummaryDto>> GetSystemAlertNotifications(string userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.RecipientId == userId && n.Type == NotificationType.SystemAlert && n.IsRead == false)
            .Include(n => n.Recipient)
            .Include(n => n.Sender)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(notification => new NotificationSummaryDto
        {
            Id = notification.Id,
            message = notification.Message ?? string.Empty,
            recipient = notification.Recipient?.UserName ?? "Unknown",
            sender = notification.Sender?.UserName ?? "Unknown",
            url = notification.Url ?? string.Empty,
            Type = notification.Type.ToString(),
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        }).ToList();
    }
    public async Task<List<NotificationSummaryDto>> GetAllNotifications(string userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.RecipientId == userId)
            .Include(n => n.Recipient)
            .Include(n => n.Sender)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        var notificationDtos = notifications.Select(notification => new NotificationSummaryDto
        {
            Id = notification.Id,
            message = notification.Message!,
            recipient = notification.Recipient?.UserName ?? "Unknown",
            sender = notification.Sender?.UserName ?? "Unknown",
            url = notification.Url!,
            Type = notification.Type.ToString(),
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        }).ToList();
        return notificationDtos;
    }
    public async Task<NotificationSummaryDto> GetNotificationByIdAsync(int id, string userId)
    {
        var notification = await _context.Notifications
            .Where(n => n.Id == id && n.RecipientId == userId)
            .Include(n => n.Recipient)
            .Include(n => n.Sender)
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException("Notification not found");
        return new NotificationSummaryDto
        {
            Id = notification.Id,
            message = notification.Message!,
            recipient = notification.Recipient!.UserName!,
            sender = notification.Sender!.UserName!,
            CreatedAt = notification.CreatedAt,
            url = notification.Url!,
            Type = notification.Type.ToString(),
            IsRead = notification.IsRead
        };
    }
    public async Task<bool> DeleteNotificationsAsync(int id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null) return false;
        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();
        return true;
    }
    public async Task CheckForMentionsAsync(string content, string senderId, int postId)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;
        var mentionedUsernames = Regex.Matches(content, @"@(\w+)")
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();

        foreach (var username in mentionedUsernames)
        {
            var mentionedUser = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == username);
            if (mentionedUser != null && mentionedUser.Id != senderId)
            {
                await SendNotification(
                    mentionedUser.Id,
                    senderId,
                    $"You were mentioned in a post.",
                    NotificationType.Mention,
                    $"/posts/{postId}"
                );
            }
        }
    }
    public async Task CheckForThreadMentionsAsync(int threadId)
    {
        var posts = await _context.Posts
            .Where(p => p.ThreadId == threadId)
            .Include(p => p.Author)
            .ToListAsync();

        foreach (var post in posts)
        {
            var mentions = ExtractMentions(post.Content ?? "");
            foreach (var username in mentions)
            {
                var mentionedUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == username);

                if (mentionedUser != null && mentionedUser.Id != post.ApplicationUserId)
                {
                    await SendNotification(
                        recipientId: mentionedUser.Id,
                        senderId: post.ApplicationUserId!,
                        message: $"You were mentioned in a post in thread #{threadId}",
                        type: NotificationType.Mention,
                        url: $"/thread/{threadId}"
                    );
                }
            }
        }
    }
    private static List<string> ExtractMentions(string content)
    {
        var matches = Regex.Matches(content, @"@(\w+)");
        return matches.Select(m => m.Groups[1].Value).ToList();
    }
}