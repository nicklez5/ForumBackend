namespace MyApi.Models;

public class CreateThreadDto
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int ForumId { get; set; }
    public IFormFile? Image { get; set; }
}