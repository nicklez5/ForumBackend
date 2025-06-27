using System.ComponentModel.DataAnnotations;

namespace MyApi.Models;

public class CreatePostDto
{
    [Required]
    public string Content { get; set; } = string.Empty;

    [Required]
    public int ThreadId { get; set; }

    public int? ParentPostId { get; set; }
}