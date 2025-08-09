namespace MyApi.Models;

public class CreateForumDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IFormFile? Icon { get; set; }
    public IFormFile? Banner { get; set; }
}