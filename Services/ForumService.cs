using MyApi.Models;
using MyApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Xml.Schema;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http.HttpResults;
namespace MyApi.Services;

public class ForumService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<bool> SubscribeUserToForumAsync(string userId, int forumId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        var forum = await _context.Forums.Include(f => f.Users).FirstOrDefaultAsync(f => f.Id == forumId);

        if (user == null || forum == null) return false;

        if (!forum.Users.Any(u => u.Id == user.Id))
        {
            forum.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        return true;
    }
    public async Task<bool> IsUserSubscribedAsync(string userId, int forumId)
    {
        var forum = await _context.Forums
            .Include(f => f.Users)
            .FirstOrDefaultAsync(f => f.Id == forumId);

        if (forum == null)
            return false;

        return forum.Users.Any(u => u.Id == userId);
    }
    public async Task<bool> UnSubscribeUserToForumAsync(string userId, int forumId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        var forum = await _context.Forums.Include(f => f.Users).FirstOrDefaultAsync(f => f.Id == forumId);

        if (user == null || forum == null) return false;

        if (forum.Users.Any(u => u.Id == user.Id))
        {
            forum.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
        return true;
    }
    public async Task<Forum> CreateForumAsync(string title, string description, string? iconUrl, string? bannerUrl, string authorId)
    {
        var forum = new Forum
        {
            Title = title,
            Description = description,
            ApplicationUserId = authorId,
            IconUrl = iconUrl,
            BannerUrl = bannerUrl,
            CreatedAt = DateTime.UtcNow
        };

        _context.Forums.Add(forum);
        await _context.SaveChangesAsync();

        return forum;
    }
    public async Task<List<ForumDto>> GetForumsCreatedByUserAsync(string username)
    {
        var forums = await _context.Forums
                .Where(f => f.Author.UserName == username)
                .Include(f => f.Threads!)
                .ThenInclude(t => t.Author)
                .ThenInclude(t => t.Posts)
                .Include(f => f.Users!)
                .ToListAsync();
        return forums.Select(f => new ForumDto
        {
            Id = f.Id,
            Title = f.Title!,
            Description = f.Description,
            IconUrl = f.IconUrl,
            BannerUrl = f.BannerUrl,
            Threads = f.Threads!.Select(t => new ThreadDto
            {
                Id = t.Id,
                Title = t.Title ?? string.Empty,
                AuthorUsername = t.Author?.UserName ?? "Unknown",
                CreatedAt = t.CreatedAt,
                Content = t.Content,
                ImageUrl = t.ImageUrl,
                ForumId = t.ForumId,
                ForumTitle = t.Forum.Title,
                ForumIconUrl = t.Forum.IconUrl,
                AuthorId = t.Author.Id,
                PostCount = t.Posts.Count(),
                LikeCount = t.Votes?.Count ?? 0,
                Posts = null,
            }).ToList(),
            Author = f.Author.UserName,
            AuthorIcon = f.Author.ProfileImageUrl,
            CreatedAt = f.CreatedAt,
            Users = f.Users!.Select(u => new UserDto
            {
                Username = u.UserName
            }).ToList(),
        }).ToList();
    }
    public async Task<List<ForumDto>> GetForumsByUserIdAsync(string userId)
    {
        var forums = await _context.Forums
            .Where(f => f.Users.Any(u => u.Id == userId))
            .Include(f => f.Threads!)
            .ThenInclude(t => t.Author)
            .ThenInclude(t => t.Posts)
            .Include(f => f.Users!)
            .ToListAsync();

        return forums.Select(f => new ForumDto
        {
            Id = f.Id,
            Title = f.Title!,
            Description = f.Description,
            IconUrl = f.IconUrl,
            BannerUrl = f.BannerUrl,
            Threads = f.Threads!.Select(t => new ThreadDto
            {
                Id = t.Id,
                Title = t.Title ?? string.Empty,
                AuthorUsername = t.Author?.UserName ?? "Unknown",
                CreatedAt = t.CreatedAt,
                Content = t.Content,
                ImageUrl = t.ImageUrl,
                ForumId = t.ForumId,
                ForumTitle = t.Forum.Title,
                ForumIconUrl = t.Forum.IconUrl,
                AuthorId = t.Author.Id,
                PostCount = t.Posts.Count(),
                LikeCount = t.Votes?.Count ?? 0,
                Posts = null,
            }).ToList(),
            Author = f.Author.UserName,
            AuthorIcon = f.Author.ProfileImageUrl,
            CreatedAt = f.CreatedAt,
            Users = f.Users!.Select(u => new UserDto
            {
                Username = u.UserName
            }).ToList(),
        }).ToList();


    }
    public async Task<List<ForumDto>> GetAllForumsAsync()
    {
        var forums = await _context.Forums
        .Include(f => f.Threads!)
            .ThenInclude(t => t.Author)
        .Include(f => f.Threads)!
            .ThenInclude(t => t.Posts)
        .Include(f => f.Users!)
        .Include(f => f.Author)
        .ToListAsync(); // Fetch from DB first

        return forums.Select(f => new ForumDto
        {
            Id = f.Id,
            Title = f.Title ?? "",
            Description = f.Description ?? "",
            IconUrl = f.IconUrl ?? "",
            BannerUrl = f.BannerUrl ?? "",
            Threads = f.Threads != null ? f.Threads.Select(t => new ThreadDto
            {
                Id = t.Id,
                Title = t.Title ?? string.Empty,
                AuthorUsername = t.Author?.UserName ?? "Unknown",
                CreatedAt = t.CreatedAt,
                Content = t.Content ?? "",
                ImageUrl = t.ImageUrl ?? "",
                ForumId = t.ForumId,
                ForumTitle = t.Forum?.Title ?? "",
                ForumIconUrl = t.Forum?.IconUrl ?? "",
                AuthorId = t.Author?.Id ?? "",
                PostCount = t.Posts?.Count ?? 0,
                LikeCount = t.Votes?.Count ?? 0,
                Posts = null
            }).ToList() : new List<ThreadDto>(),
            Author = f.Author?.UserName ?? "Unknown",
            AuthorIcon = f.Author?.ProfileImageUrl ?? "Unknown",
            CreatedAt = f.CreatedAt,
            Users = f.Users?.Select(u => new UserDto
            {
                Username = u.UserName
            }).ToList() ?? new List<UserDto>(),
        }).ToList();
    }
    public async Task<ForumDto> GetForumByIdAsync(int id)
    {
        var forum = await _context.Forums
            .Include(f => f.Threads!)
            .ThenInclude(t => t.Author)
            .Include(f => f.Threads!)
            .ThenInclude(t => t.Posts)
            .Include(f => f.Users!)
            .Include(f => f.Author)
            .FirstOrDefaultAsync(f => f.Id == id) ?? throw new KeyNotFoundException("Forum not found");

        return new ForumDto
        {
            Id = forum.Id,
            Title = forum.Title ?? "",
            Description = forum.Description ?? "",
            BannerUrl = forum.BannerUrl ?? "",
            IconUrl = forum.IconUrl ?? "",
            Threads = forum.Threads != null ? forum.Threads.Select(t => new ThreadDto
            {
                Id = t.Id,
                Title = t.Title ?? string.Empty,
                AuthorUsername = t.Author?.UserName ?? "Unknown",
                CreatedAt = t.CreatedAt,
                Content =  t.Content ?? "",
                ImageUrl = t.ImageUrl ?? "",
                ForumId = t.ForumId,
                ForumTitle = t.Forum?.Title ?? "",
                ForumIconUrl = t.Forum?.IconUrl ?? "",
                AuthorId = t.Author?.Id ?? "",
                PostCount = t.Posts.Count,
                LikeCount = t.Votes?.Count ?? 0,
                Posts = null,
            }).ToList() : new List<ThreadDto>(),
            Author = forum.Author?.UserName ?? "",
            AuthorIcon = forum.Author?.ProfileImageUrl ?? "",
            Users = forum.Users?.Select(u => new UserDto
            {
                Username = u.UserName
            }).ToList() ?? new List<UserDto>(),
        };
    }
    public async Task<bool> UpdateForumAsync(int id, string title, string description, string? bannerUrl, string? iconUrl, bool removeBanner = false, bool removeIcon = false)
    {
        var forum = await _context.Forums.FindAsync(id);
        if (forum == null) return false;
        forum.Title = title ?? forum.Title;
        forum.Description = description ?? forum.Description;
        if (removeBanner)
            forum.BannerUrl = null;
        else if (bannerUrl != null)
            forum.BannerUrl = bannerUrl;

        if (removeIcon)
            forum.IconUrl = null;
        else if (iconUrl != null)
            forum.IconUrl = iconUrl;
        
        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<bool> DeleteForumAsync(int id)
    {
        var forum = await _context.Forums
            .Include(f => f.Users)
            .Include(f => f.Threads)
                .ThenInclude(t => t.Posts)
                    .ThenInclude(p => p.Votes)
            .Include(f => f.Threads)
                .ThenInclude(t => t.Votes)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (forum == null) return false;

        // 1. Remove Users (many-to-many)
        forum.Users.Clear();

        // 2. Delete child objects
        foreach (var thread in forum.Threads)
        {
            foreach (var post in thread.Posts)
            {
                _context.PostVotes.RemoveRange(post.Votes);
            }

            _context.Posts.RemoveRange(thread.Posts);
            _context.ThreadVotes.RemoveRange(thread.Votes);
        }

        _context.Threads.RemoveRange(forum.Threads);

        // 3. Remove forum
        _context.Forums.Remove(forum);

        await _context.SaveChangesAsync();

        return true;
    }

    
    private List<PostDto> BuildReplyTree(List<Post> allReplies,Dictionary<int,int> likeCounts, int? parentId = null)
    {
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
                LikeCount = likeCounts.GetValueOrDefault(r.Id, 0),
                Replies = BuildReplyTree(allReplies,likeCounts, r.Id)
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
            ForumIconUrl = t.Forum.IconUrl,
            ForumTitle = t.Forum!.Title!,
            AuthorId = t.ApplicationUserId,
            AuthorUsername = t.Author!.UserName!,
            PostCount = t.Posts!.Count,
            LikeCount = t.Votes?.Count ?? 0,
            CreatedAt = t.CreatedAt,
            Posts = null,
        }).ToList();

        var posts = await _context.Posts
            .Where(p => p.Content.Contains(query))
            .Include(p => p.Author)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        var postIds = posts.Select(p => p.Id).ToList();

        var likeCountz = await _context.PostVotes
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
            Replies = BuildReplyTree(replies,likeCountz, post.Id)
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