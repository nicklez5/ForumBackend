using MyApi.Models;
using MyApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Xml.Schema;
using Microsoft.Identity.Client;
namespace MyApi.Services;

public class ForumService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Forum> CreateForumAsync(string title, string description, string? imageUrl)
    {
        var forum = new Forum
        {
            Title = title,
            Description = description,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow
        };

        _context.Forums.Add(forum);
        await _context.SaveChangesAsync();

        return forum;
    }

    public async Task<List<ForumDto>> GetAllForumsAsync()
    {
        var forums = await _context.Forums
        .Include(f => f.Threads!)
            .ThenInclude(t => t.Author)
        .ToListAsync(); // Fetch from DB first

        return forums.Select(f => new ForumDto
        {
            Id = f.Id,
            Title = f.Title!,
            Description = f.Description,
            ImageUrl = f.ImageUrl,
            Threads = f.Threads!.Select(t => new ThreadSummaryDto
            {
                Id = t.Id,
                Title = t.Title ?? string.Empty,
                AuthorUsername = t.Author?.UserName ?? "Unknown",
                CreatedAt = t.CreatedAt
            }).ToList()
        }).ToList();
    }
    public async Task<ForumDto> GetForumByIdAsync(int id)
    {
        var forum = await _context.Forums
            .Include(f => f.Threads!)
            .ThenInclude(t => t.Author)
            .FirstOrDefaultAsync(f => f.Id == id) ?? throw new KeyNotFoundException("Forum not found");

        return new ForumDto
        {
            Id = forum.Id,
            Title = forum.Title!,
            Description = forum.Description,
            ImageUrl = forum.ImageUrl,
            Threads = forum.Threads!.Select(t => new ThreadSummaryDto
            {
                Id = t.Id,
                Title = t.Title ?? string.Empty,
                AuthorUsername = t.Author?.UserName ?? "Unknown",
                CreatedAt = t.CreatedAt
            }).ToList()
        };
    }
    public async Task<bool> UpdateForumAsync(int id, string title, string description, string? imageUrl)
    {
        var forum = await _context.Forums.FindAsync(id);
        if (forum == null) return false;
        forum.Title = title ?? forum.Title;
        forum.Description = description ?? forum.Description;
        forum.ImageUrl = imageUrl ?? forum.ImageUrl;
        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<bool> DeleteForumAsync(int id)
    {
        var forum = await _context.Forums.FindAsync(id);
        if (forum == null) return false;

        _context.Forums.Remove(forum);
        await _context.SaveChangesAsync();
        return true;
    }
    private List<PostDto> BuildReplyTree(List<Post> allReplies, int? parentId = null) {
        return allReplies
            .Where(r => r.ParentPostId == parentId)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new PostDto
            {
                Id = r.Id,
                Content = r.Content!,
                AuthorUsername = r.Author?.UserName ?? "Unknown",
                ThreadId = r.ThreadId,
                ParentPostId = r.ParentPostId,
                CreatedAt = r.CreatedAt,
                LikeCount = _context.PostLikes.Count(pl => pl.PostId == r.Id),
                Replies = BuildReplyTree(allReplies, r.ParentPostId)
            }).ToList();
    }
    public async Task<SearchResult> SearchContentAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new SearchResult { Threads = [], Posts = [] };

        var threadz = await _context.Threads
            .Where(t => t.Title.Contains(query) || t.Content.Contains(query))
            .Include(t => t.Forum)
            .Include(t => t.Posts!)
            .ThenInclude(p => p.Author)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        
        var threadDtos = threadz.Select(t => new ThreadDto
        {
            Id = t.Id,
            Title = t.Title!,
            Content = t.Content,
            ForumId = t.ForumId,
            ForumTitle = t.Forum!.Title!,
            AuthorId = t.ApplicationUserId,
            AuthorUsername = t.Author!.UserName!,
            PostCount = t.Posts!.Count,
            LikeCount = t.Likes?.Count ?? 0,
            CreatedAt = t.CreatedAt,
            Posts = null,
        }).ToList();

        var posts = await _context.Posts
            .Where(p => p.Content.Contains(query))
            .Include(p => p.Author)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        var postIds = posts.Select(p => p.Id).ToList();

        var likeCountz = await _context.PostLikes
            .Where(pl => postIds.Contains(pl.PostId))
            .GroupBy(pl => pl.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count);

        var replies = await _context.Posts
            .Where(p => p.ParentPostId != null && postIds.Contains(p.ParentPostId.Value))
            .Include(r => r.Author)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();


        var postDtos = posts.Select(post => new PostDto
        {
            Id = post.Id,
            Content = post.Content!,
            AuthorUsername = post.Author?.UserName ?? "Unknown",
            ThreadId = post.ThreadId,
            CreatedAt = post.CreatedAt,
            LikeCount = likeCountz.GetValueOrDefault(post.Id, 0),
            Replies = BuildReplyTree(replies, post.Id)
        }).ToList();

        return new SearchResult
        {
            Threads = threadDtos,
            Posts = postDtos
        };
    }
}
public class SearchResult
{
    public List<ThreadDto> Threads { get; set; } = [];
    public List<PostDto> Posts { get; set; } = [];
}