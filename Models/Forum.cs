using System.ComponentModel.DataAnnotations;
using Microsoft.Identity.Client;

namespace MyApi.Models;

public class Forum
{
    public int Id { get; set; }

    [Required]
    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? BannerUrl { get; set; }
    public string? IconUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ApplicationUserId { get; set; }
    public ApplicationUser? Author { get; set; }
    public ICollection<Threads> Threads { get; set; }  = new List<Threads>();

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}