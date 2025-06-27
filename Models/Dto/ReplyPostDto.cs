namespace MyApi.Models;

public class ReplyPostDto
{
    public int threadId { get; set; }
    public int parentPostId { get; set; }
    public string Content { get; set; } = string.Empty;
}