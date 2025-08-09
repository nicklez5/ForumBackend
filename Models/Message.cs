using MyApi.Models;
namespace MyApi.Models;
public class Message
{
    public int Id { get; set; }
    public string? SenderId { get; set; }

    public string? RecipientId { get; set; }

    public string? Content { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; } = false;

    public ApplicationUser? Sender { get; set; }

    public ApplicationUser? Recipient { get; set; }
}
public class MessageDto
{
    public int Id { get; set; }
    public string? SenderId { get; set; }

    public string? SenderUsername { get; set; }

    public string? SenderProfileImageUrl { get; set; }

    public string? RecipientId { get; set; }

    public string? RecipientUsername { get; set; }

    public string? RecipientProfileImageUrl { get; set; }

    public string? Content { get; set; }

    public DateTime SentAt { get; set; }
}
public class CreateMessageDto
{
    public string RecipientId { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}
public class MessagesDto
{
    public string? RecipientId { get; set; }
    public string? Content { get; set; }
}
public class EditMessageDto
{
    public string? Content { get; set; }
}