namespace MyApi.Models;

public class UserActivityDto
{
    public List<PostDto> Posts { get; set; } = new();

    public List<ThreadDto> Threads { get; set; } = new();

    public List<ForumDto> Forums { get; set; } = new();

    public int TotalPostLikeCount { get; set; }

    public int TotalThreadLikeCount { get; set; }

    public int TotalSubscribedForumCount { get; set; } 

}