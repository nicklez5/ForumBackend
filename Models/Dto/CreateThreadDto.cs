namespace MyApi.Models;

public class CreateThreadDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public int ForumId { get; set; }
}