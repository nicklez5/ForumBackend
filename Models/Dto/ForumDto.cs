namespace MyApi.Models;

public class ForumDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BannerUrl { get; set; }
    public string? IconUrl { get; set; }
    public string? Author { get; set; }
    public string? AuthorIcon { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ThreadDto> Threads { get; set; } = new();

    public List<UserDto> Users { get; set; } = new();
}
public class ForumSmallDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<ThreadSummaryDto> Threads { get; set; } = new();

    public List<UserDto> Users { get; set; } = new();
}