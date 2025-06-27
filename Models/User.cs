using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
namespace MyApi.Models;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }


    public string? Bio { get; set; }

    public string? ProfileImageUrl { get; set; }

    public DateTime DateJoined { get; set; } = DateTime.UtcNow;

    public int PostCount { get; set; }
    public int Reputation { get; set; }

    public bool IsModerator { get; set; }

    public bool IsBanned { get; set; }

    public DateTime? BannedAt { get; set; }
    public ICollection<Post>? Posts { get; set; }
    public ICollection<Threads>? Threads { get; set; }

    public ICollection<Notification>? Notifications { get; set; }

    public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}