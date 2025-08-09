namespace MyApi.Models;

public class ReplyPostDto
{
    public int ThreadId { get; set; }
    public int ParentPostId { get; set; }
    public string? Content { get; set; }

    public IFormFile? Image { get; set; }
}