using System.ComponentModel.DataAnnotations;

namespace MyApi.Models;

public class EditPostDto
{
    [Required]
    public string Content { get; set; } = string.Empty;
}