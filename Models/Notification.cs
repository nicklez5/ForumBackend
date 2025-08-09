using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;
namespace MyApi.Models;

public enum NotificationType
{
    Reply,
    Mention,
    Like,
    ModeratorAction,
    SystemAlert,
    Message
}

public class Notification
{
    public int Id { get; set; }

    public string? RecipientId { get; set; }

    public ApplicationUser? Recipient { get; set; }

    public string? SenderId { get; set; }
    public string? Url { get; set; }
    public ApplicationUser? Sender { get; set; }

    [Required]
    public string? Message { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; } = false;

    public NotificationType Type { get; set; }
}