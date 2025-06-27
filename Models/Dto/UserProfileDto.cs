namespace MyApi.Models;

public class UserProfileDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime DateJoined { get; set; }
    public int PostCount { get; set; }
    public int Reputation { get; set; }
    public bool IsModerator { get; set; }
    public bool IsBanned { get; set; }

    public DateTime? BannedAt { get; set; }
}