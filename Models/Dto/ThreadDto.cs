namespace MyApi.Models;

public class ThreadDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public int ForumId { get; set; }
    public string? ForumTitle { get; set; }
    public string? ForumIconUrl { get; set; }
    public string? AuthorId { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;

    public int PostCount { get; set; }

    public int LikeCount { get; set; }

    public int UserVote { get; set; }
    public List<PostDto>? Posts { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class ThreadsDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ForumTitle { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
    public int PostCount { get; set; }
    public int LikeCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
public class ThreadSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AuthorUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
public class ThreadDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string AuthorUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}