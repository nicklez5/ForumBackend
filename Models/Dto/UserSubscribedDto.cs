namespace MyApi.Models;

public class UserSubscribedDto
{
    public List<ForumDto> forums { get; set; } = new();
    public List<ThreadDto> threads { get; set; } = new();
}