namespace MyApi.Models;

public class PostDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;

    public string AuthorUsername { get; set; } = string.Empty;

    public int ThreadId { get; set; }
    public DateTime CreatedAt { get; set; }

    public int LikeCount { get; set; }
    public List<ReplyDto> Replies { get; set; } = new();
}