namespace MyApi.Models;

public class UserActivityDto
{
    public List<PostDto> Posts { get; set; } = new();

    public List<ThreadDto> Threads { get; set; } = new();
}