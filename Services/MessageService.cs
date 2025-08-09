using MyApi.Models;
using MyApi.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using Microsoft.AspNetCore.Identity;
namespace MyApi.Services;

public interface IMessageService
{
    Task<MessageDto> SendMessageAsync(string senderId, string recipientId, string content);
    Task<List<MessageDto>> GetMessagesAsync(string currentUserId, string contactId);
    Task<List<MessageDto>> GetAllUserMessagesAsync(string currentUserId);
    Task<Message?> EditMessageAsync(int messageId, string userId, string newContent);
    Task<bool> DeleteMessageAsync(int messageId, string userId);
}
public class MessageService(ApplicationDbContext context, UserManager<ApplicationUser> userManager , NotificationService notificationService) : IMessageService
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    private readonly NotificationService _notificationService = notificationService;
    public async Task<List<MessageDto>> GetAllUserMessagesAsync(string currentUserId)
    {
        return await _context.Messages
                .Where(m => m.SenderId == currentUserId || m.RecipientId == currentUserId)
                .OrderByDescending(m => m.SentAt)
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderUsername = m.Sender!.UserName,
                    SenderProfileImageUrl = m.Sender!.ProfileImageUrl,
                    RecipientId = m.RecipientId,
                    RecipientUsername = m.Recipient!.UserName,
                    RecipientProfileImageUrl = m.Recipient!.ProfileImageUrl,
                    Content = m.Content,
                    SentAt = m.SentAt
                })
                .ToListAsync();
    }

    public async Task<List<MessageDto>> GetMessagesAsync(string currentUserId, string contactId)
    {
        return await _context.Messages
                .Where(m =>
                (m.SenderId == currentUserId && m.RecipientId == contactId) ||
                (m.SenderId == contactId && m.RecipientId == currentUserId))
                .OrderBy(m => m.SentAt)
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderUsername = m.Sender!.UserName,
                    SenderProfileImageUrl = m.Sender!.ProfileImageUrl,
                    RecipientId = m.RecipientId,
                    RecipientProfileImageUrl = m.Recipient!.ProfileImageUrl,
                    RecipientUsername = m.Recipient!.UserName,
                    Content = m.Content,
                    SentAt = m.SentAt
                })
                .ToListAsync();
    }

    public async Task<MessageDto> SendMessageAsync(string senderId, string recipientId, string content)
    {
        var sender = await _userManager.FindByIdAsync(senderId);
        var recipient = await _userManager.FindByIdAsync(recipientId);
        var message = new Message
        {
            SenderId = senderId,
            RecipientId = recipientId,
            Content = content,
            SentAt = DateTime.UtcNow
        };
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        await _notificationService.SendNotification(
            recipientId: recipient.Id,
            senderId: sender.Id,
            message: content,
            type: NotificationType.Message,
            url: $"/messages/${message.Id}"
        );
        return new MessageDto
        {
            Id = message.Id,
            SenderId = sender.Id,
            SenderProfileImageUrl = sender.ProfileImageUrl,
            SenderUsername = sender.UserName,
            RecipientId = recipient.Id,
            RecipientProfileImageUrl = recipient.ProfileImageUrl,
            RecipientUsername = recipient.UserName,
            Content = message.Content,
            SentAt = message.SentAt
        };
    }
    public async Task<Message?> EditMessageAsync(int messageId, string userId, string newContent)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message == null || message.SenderId != userId)
            return null;

        message.Content = newContent;
        await _context.SaveChangesAsync();
        return message;
    }
    public async Task<bool> DeleteMessageAsync(int messageId, string userId)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message == null || message.SenderId != userId)
            return false;

        _context.Messages.Remove(message);
        await _context.SaveChangesAsync();
        return true;
    }
    
}