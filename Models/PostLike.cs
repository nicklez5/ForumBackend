namespace MyApi.Models;

public class PostLike
{
    public int Id { get; set; }
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public int PostId { get; set; }
    public Post? Post { get; set; }

    public DateTime LikedAt { get; set; } = DateTime.UtcNow;
}