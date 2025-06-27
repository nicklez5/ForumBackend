namespace MyApi.Models;

public class ForumDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<ThreadSummaryDto> Threads { get; set; } = new();
}