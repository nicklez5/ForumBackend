namespace MyApi.Models;

public class ReplyDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorUsername { get; set; } = string.Empty;

    public List<ReplyDto> Replies { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}