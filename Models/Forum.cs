using System.ComponentModel.DataAnnotations;
using Microsoft.Identity.Client;

namespace MyApi.Models;

public class Forum
{
    public int Id { get; set; }

    [Required]
    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Threads>? Threads { get; set; }
}