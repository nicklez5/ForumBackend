namespace MyApi.Models;

public class EditForumDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public IFormFile? Banner { get; set; }
    public bool RemoveBanner { get; set; }
    public IFormFile? Icon { get; set; }
    public bool RemoveIcon { get; set; }
}