namespace MyApi.Models;

public class NotificationDto
{
    public int Id { get; set; }
    public string recipientId { get; set; } = string.Empty;
    public string senderId { get; set; } = string.Empty;

    public string message { get; set; } = string.Empty;
    public string url { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;
 }
public class NotificationSummaryDto
{
    public int Id { get; set; }
    public string recipient { get; set; } = string.Empty;

    public string sender { get; set; } = string.Empty;

    public string message { get; set; } = string.Empty;

    public string url { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;
 }