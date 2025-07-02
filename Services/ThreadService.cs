using MyApi.Models;
using MyApi.Data;
using MyApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace MyApi.Services;

public class ThreadService(ApplicationDbContext context, NotificationService notificationService)
{
    private readonly ApplicationDbContext _context = context;
    private readonly NotificationService _notificationService = notificationService;

    public async Task<ThreadDto> CreateThreadAsync(string title, string content, int forumId, string authorId)
    {
        var thread = new Threads
        {
            Title = title,
            Content = content,
            ForumId = forumId,
            ApplicationUserId = authorId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Threads.Add(thread);
        var user = await _context.Users.FindAsync(authorId);
        if (user != null)
        {
            user.Reputation += 10;
        }
        await _context.SaveChangesAsync();
        await _context.Entry(thread).Reference(t => t.Author).LoadAsync();
        await _context.Entry(thread).Reference(t => t.Forum).LoadAsync();
        await _notificationService.CheckForThreadMentionsAsync(thread.Id);
        return new ThreadDto
        {
            Id = thread.Id,
            Title = thread.Title,
            Content = content,
            ForumId = thread.ForumId,
            ForumImageUrl = thread.Forum?.ImageUrl ?? "Unknown",
            ForumTitle = thread.Forum!.Title,
            AuthorId = thread.ApplicationUserId,
            AuthorUsername = thread.Author?.UserName ?? "Unknown",
            PostCount = thread.Posts?.Count ?? 0,
            LikeCount = thread.Likes?.Count ?? 0,
            CreatedAt = thread.CreatedAt,
            Posts = null
        };
    }
    public async Task<List<ThreadDto>> GetThreadsByUserAsync(string userId)
    {
        var threads = await _context.Threads
            .Where(t => t.ApplicationUserId == userId)
            .Include(t => t.Forum)
            .Include(t => t.Posts!)
                .ThenInclude(p => p.Author)
            .Include(t => t.Author)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var allPosts = threads.SelectMany(t => t.Posts!).ToList();
        var postIds = allPosts.Select(p => p.Id).ToList();

        var likeCounts = await _context.PostLikes
            .Where(pl => postIds.Contains(pl.PostId))
            .GroupBy(pl => pl.PostId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var threadDtos = new List<ThreadDto>();

        foreach (var t in threads)
        {
            var posts = t.Posts!;
            var topLevel = posts.Where(p => p.ParentPostId == null).ToList();
            var replies = posts.Where(p => p.ParentPostId != null).ToList();

            var postDtos = topLevel.Select(p => new PostDto
            {
                Id = p.Id,
                Content = p.Content!,
                AuthorUsername = p.Author?.UserName ?? "Unknown",
                ThreadId = p.ThreadId,
                ParentPostId = null,
                CreatedAt = p.CreatedAt,
                LikeCount = likeCounts.GetValueOrDefault(p.Id, 0),
                Replies = BuildReplyTree(replies, likeCounts, p.Id)
            }).ToList();

            threadDtos.Add(new ThreadDto
            {
                Id = t.Id,
                Title = t.Title!,
                Content = t.Content,
                ForumId = t.ForumId,
                ForumTitle = t.Forum?.Title ?? "Unknown",
                ForumImageUrl = t.Forum?.ImageUrl ?? "Unknown",
                AuthorId = t.ApplicationUserId,
                AuthorUsername = t.Author?.UserName ?? "Unknown",
                PostCount = posts.Count,
                LikeCount = t.Likes?.Count ?? 0,
                CreatedAt = t.CreatedAt,
                Posts = postDtos
            });
        }

        return threadDtos;
    }
    public async Task<List<ThreadDto>> GetAllThreads()
    {
        var threads = await _context.Threads
            .Include(t => t.Author)
            .Include(t => t.Forum)
            .Include(t => t.Posts)
            .ToListAsync();

        var threadIds = threads.Select(t => t.Id).ToList();

        var threadLikes = await _context.ThreadLikes
            .Where(tl => threadIds.Contains(tl.ThreadId))
            .GroupBy(tl => tl.ThreadId)
            .Select(g => new { ThreadId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ThreadId, x => x.Count);

        var result = threads.Select(t => new ThreadDto
        {
            Id = t.Id,
            Title = t.Title,
            AuthorUsername = t.Author?.UserName ?? "Unknown",
            CreatedAt = t.CreatedAt,
            PostCount = t.Posts.Count,
            ForumId = t.ForumId,
            ForumImageUrl = t.Forum?.ImageUrl ?? "Unknown",
            ForumTitle = t.Forum?.Title ?? "Unknown",
            LikeCount = threadLikes.GetValueOrDefault(t.Id, 0),
            Posts = []
        }).ToList();
        return result;
    }
    public async Task<List<ThreadDto>> GetThreadsByForumAsync(int forumId)
    {
        var threads = await _context.Threads
            .Where(t => t.ForumId == forumId)
            .Include(t => t.Author)
            .Include(t => t.Forum)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var threadIds = threads.Select(t => t.Id).ToList();

        var posts = await _context.Posts
            .Where(p => threadIds.Contains(p.ThreadId))
            .Include(p => p.Author)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        var likeCounts = await _context.PostLikes
            .Where(pl => threadIds.Contains(pl.Post.ThreadId))
            .GroupBy(pl => pl.PostId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count);
        var threadLikeCounts = await _context.ThreadLikes
            .Where(tl => threadIds.Contains(tl.ThreadId))
            .GroupBy(tl => tl.ThreadId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count);
        // group posts by threadId
        var postsByThread = posts.GroupBy(p => p.ThreadId).ToDictionary(g => g.Key, g => g.ToList());

        List<ThreadDto> threadDtos = new();

        foreach (var thread in threads)
        {
            var allPosts = postsByThread.GetValueOrDefault(thread.Id, new List<Post>());
            var topLevel = allPosts.Where(p => p.ParentPostId == null).ToList();
            var replies = allPosts.Where(p => p.ParentPostId != null).ToList();

            var postDtos = topLevel.Select(p => new PostDto
            {
                Id = p.Id,
                Content = p.Content!,
                AuthorUsername = p.Author?.UserName ?? "Unknown",
                ThreadId = p.ThreadId,
                ParentPostId = null,
                CreatedAt = p.CreatedAt,
                LikeCount = likeCounts.GetValueOrDefault(p.Id, 0),
                Replies = BuildReplyTree(replies, likeCounts, p.Id)
            }).ToList();

            threadDtos.Add(new ThreadDto
            {
                Id = thread.Id,
                Title = thread.Title,
                Content = thread.Content,
                ForumId = thread.ForumId,
                ForumTitle = thread.Forum?.Title ?? "Unknown",
                ForumImageUrl = thread.Forum?.ImageUrl ?? "Unknown",
                AuthorId = thread.ApplicationUserId,
                AuthorUsername = thread.Author?.UserName ?? "Unknown",
                CreatedAt = thread.CreatedAt,
                LikeCount = threadLikeCounts.GetValueOrDefault(thread.Id, 0),
                PostCount = allPosts.Count,
                Posts = postDtos
            });
        }

        return threadDtos;
    }
    private List<PostDto> BuildReplyTree(List<Post> allReplies, Dictionary<int, int> likeCounts, int? parentId = null)
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
                Replies = BuildReplyTree(allReplies, likeCounts, r.Id)
            }).ToList();
    }

    public async Task<ThreadDto> GetThreadByIdAsync(int threadId)
    {
        var thread = await _context.Threads
            .Include(t => t.Author)
            .Include(t => t.Forum)
            .FirstOrDefaultAsync(t => t.Id == threadId) ?? throw new KeyNotFoundException("Thread not found");

        var allPosts = await _context.Posts
            .Where(p => p.ThreadId == threadId)
            .Include(p => p.Author)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        var likeCounts = await _context.PostLikes
            .Where(pl => allPosts.Select(p => p.Id).Contains(pl.PostId))
            .GroupBy(pl => pl.PostId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var topLevelPosts = allPosts.Where(p => p.ParentPostId == null).ToList();

        var postsDto = topLevelPosts.Select(p => new PostDto
        {
            Id = p.Id,
            Content = p.Content!,
            AuthorUsername = p.Author?.UserName ?? "Unknown",
            ThreadId = p.ThreadId,
            ParentPostId = p.ParentPostId,
            CreatedAt = p.CreatedAt,
            LikeCount = likeCounts.GetValueOrDefault(p.Id, 0),
            Replies = BuildReplyTree(allPosts, likeCounts, p.Id)
        }).ToList();

        return new ThreadDto
        {
            Id = thread.Id,
            Title = thread.Title,
            Content = thread.Content!,
            ForumId = thread.ForumId,
            ForumTitle = thread.Forum.Title,
            ForumImageUrl = thread.Forum?.ImageUrl ?? "Unknown",
            AuthorId = thread.ApplicationUserId!,
            AuthorUsername = thread.Author?.UserName ?? "Unknown",
            PostCount = allPosts.Count,
            LikeCount = likeCounts.Where(kv => allPosts.Any(p => p.Id == kv.Key)).Sum(x => x.Value),
            CreatedAt = thread.CreatedAt,
            Posts = postsDto
        };
    }

    public async Task<bool> UpdateThreadAsync(int id, string title, string content)
    {
        var thread = await _context.Threads.FindAsync(id);
        if (thread == null) return false;

        thread.Title = title ?? thread.Title;
        thread.Content = content ?? thread.Content;
        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<bool> DeleteThreadAsync(int id)
    {
        var thread = await _context.Threads.FindAsync(id);
        if (thread == null) return false;

        _context.Threads.Remove(thread);
        await _context.SaveChangesAsync();
        return true;
    }
    public async Task ToggleThreadLikeAsync(int threadId, string userId)
    {
        var existingLike = await _context.ThreadLikes.FirstOrDefaultAsync(l => l.ThreadId == threadId && l.ApplicationUserId == userId);
        if (existingLike != null)
        {
            _context.ThreadLikes.Remove(existingLike);
        }
        else
        {
            _context.ThreadLikes.Add(new ThreadLike { ThreadId = threadId, ApplicationUserId = userId });
            var thread = await _context.Threads.Include(t => t.Author).FirstOrDefaultAsync(t => t.Id == threadId);
            if (thread != null && thread.ApplicationUserId != userId)
            {
                await _notificationService.SendNotification(
                    recipientId: thread.ApplicationUserId!,
                    senderId: userId,
                    message: "Your thread was liked!",
                    type: NotificationType.Like,
                    url: $"/threads/{threadId}"
                );
            }
        }
        await _context.SaveChangesAsync();
    }
    public async Task<int> GetThreadLikeCountAsync(int threadId)
    {
        return await _context.ThreadLikes
            .CountAsync(tl => tl.ThreadId == threadId);
    }
    public async Task<List<ThreadDto>> SearchThreads(string sortBy = "new")
    {
        var threads = await _context.Threads
            .Include(t => t.Author)
            .Include(t => t.Forum)
            .Include(t => t.Posts)
            .Include(t => t.Likes)
            .ToListAsync();

        var result = threads.Select(t => new ThreadDto
        {
            Id = t.Id,
            Title = t.Title,
            Content = t.Content,
            AuthorUsername = t.Author?.UserName ?? "Unknown",
            CreatedAt = t.CreatedAt,
            ForumId = t.ForumId,
            ForumTitle = t.Forum?.Title ?? "Unknown",
            ForumImageUrl = t.Forum?.ImageUrl ?? "Unknown",
            PostCount = t.Posts?.Count ?? 0,
            LikeCount = t.Likes?.Count ?? 0
        });
        var sorted = (sortBy.ToLower() switch
        {
            "best" => result.OrderByDescending(t => t.PostCount),
            "hot" => result.OrderByDescending(t => t.LikeCount),
            _ => result.OrderByDescending(t => t.CreatedAt)
        }).ToList();

        return sorted;
    }
}