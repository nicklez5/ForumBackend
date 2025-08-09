using System.ComponentModel.DataAnnotations;

namespace MyApi.Models;

public class EditPostDto
{
    [Required]
    public string Content { get; set; } = string.Empty;

    public IFormFile? Image { get; set; }
    public bool RemoveImage { get; set; }
}