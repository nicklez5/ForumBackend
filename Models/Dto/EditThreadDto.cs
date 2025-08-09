namespace MyApi.Models;

public class EditThreadDto
{
    public string Title { get; set; } = string.Empty;
    public IFormFile? Image { get; set; }
    public bool RemoveImage { get; set; }
    public string Content { get; set; } = string.Empty;
}