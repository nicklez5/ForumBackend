using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;

namespace MyApi.Models;

public class Post
{
    public int Id { get; set; }

    [Required]
    public string? Content { get; set; }

    public string? ImageUrl { get; set; }

    public string? ApplicationUserId { get; set; }
    public ApplicationUser? Author { get; set; }

    public Threads? Thread { get; set; }
    public int ThreadId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? ParentPostId { get; set; }
    public Post? ParentPost { get; set; }
    public int LikeCount { get; set; }
    public ICollection<PostVote> Votes { get; set; } = new List<PostVote>();

    public ICollection<Post> Replies { get; set; } = new List<Post>();
}