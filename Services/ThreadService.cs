using MyApi.Models;
using MyApi.Data;
using MyApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.EntityFrameworkCore.Metadata;
namespace MyApi.Services;

public class ThreadService(ApplicationDbContext context, NotificationService notificationService)
{
    private readonly ApplicationDbContext _context = context;
    private readonly NotificationService _notificationService = notificationService;

    public async Task<ThreadDto> CreateThreadAsync(string title, string content, int forumId, string authorId, string? imageUrl = null)
    {
        var thread = new Threads
        {
            Title = title,
            Content = content,
            ForumId = forumId,
            ApplicationUserId = authorId,
            ImageUrl = imageUrl,
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
        await _context.Entry(thread).Collection(t => t.Posts!).LoadAsync();
        await _context.Entry(thread).Collection(t => t.Votes!).LoadAsync();

        await _notificationService.CheckForThreadMentionsAsync(thread.Id);
        return new ThreadDto
        {
            Id = thread.Id,
            Title = thread.Title,
            Content = content,
            ForumId = thread.ForumId,
            ForumIconUrl = thread.Forum?.IconUrl ?? "Unknown",
            ForumTitle = thread.Forum!.Title,
            AuthorId = thread.ApplicationUserId,
            AuthorUsername = thread.Author?.UserName ?? "Unknown",
            PostCount = thread.Posts?.Count ?? 0,
            LikeCount = thread.Votes?.Sum(v => v.Value) ?? 0,
            ImageUrl = thread.ImageUrl,
            UserVote = 0,
            CreatedAt = thread.CreatedAt,
            Posts = null
        };
    }
    public async Task<List<ThreadDto>> GetThreadsByUserAsync(string userId, string? viewerUserId = null)
    {
        var threads = await _context.Threads
            .Where(t => t.ApplicationUserId == userId)
            .Include(t => t.Forum)
            .Include(t => t.Author)
            .Include(t => t.Votes)
            .Include(t => t.Posts!)
                .ThenInclude(p => p.Author)
            .Include(t => t.Posts!)
            .ThenInclude(p => p.Votes)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var allPosts = threads.SelectMany(t => t.Posts!).ToList();
        var postLikeCounts = allPosts.ToDictionary(
            p => p.Id,
            p => p.Votes?.Sum(v => v.Value) ?? 0
        );

        var threadDtos = new List<ThreadDto>();

        foreach (var t in threads)
        {
            var threadUserVote = viewerUserId != null ? t.Votes.FirstOrDefault(v => v.UserId == viewerUserId)?.Value ?? 0 : 0;
            var threadLikeCount = t.Votes.Sum(v => v.Value);

            var posts = t.Posts!;
            var topLevel = posts.Where(p => p.ParentPostId == null).ToList();
            var replies = posts.Where(p => p.ParentPostId != null).ToList();

            var postDtos = topLevel.Select(p =>
            {
                var postUserVote = viewerUserId != null ? p.Votes.FirstOrDefault(v => v.UserId == viewerUserId)?.Value ?? 0 : 0;
                return new PostDto
                {
                    Id = p.Id,
                    Content = p.Content!,
                    AuthorUsername = p.Author?.UserName ?? "Unknown",
                    ProfileImageUrl = p.Author?.ProfileImageUrl ?? "https://www.redditstatic.com/avatars/avatar_default_02_FF4500.png",
                    ThreadId = p.ThreadId,
                    ImageUrl = p.ImageUrl,
                    ParentPostId = null,
                    CreatedAt = p.CreatedAt,
                    LikeCount = postLikeCounts.GetValueOrDefault(p.Id, 0),
                    UserVote = postUserVote,
                    Replies = []
                };
            }).ToList();

            threadDtos.Add(new ThreadDto
            {
                Id = t.Id,
                Title = t.Title!,
                Content = t.Content,
                ForumId = t.ForumId,
                ForumTitle = t.Forum?.Title ?? "Unknown",
                ForumIconUrl = t.Forum?.IconUrl ?? "Unknown",
                AuthorId = t.ApplicationUserId,
                AuthorUsername = t.Author?.UserName ?? "Unknown",
                ImageUrl = t.ImageUrl,
                PostCount = posts.Count,
                LikeCount = threadLikeCount,
                UserVote = threadUserVote,
                CreatedAt = t.CreatedAt,
                Posts = postDtos
            });
        }

        return threadDtos;
    }
    public async Task<List<ThreadDto>> GetAllThreads(string? userId = null)
    {
        var threads = await _context.Threads
            .Include(t => t.Author)
            .Include(t => t.Forum)
            .Include(t => t.Votes)
            .Include(t => t.Posts)
            .ThenInclude(p => p.Author)
            .Include(t => t.Posts)
            .ThenInclude(p => p.Votes)
            .ToListAsync();

        var threadIds = threads.Select(t => t.Id).ToList();
        var posts = await _context.Posts
            .Where(p => threadIds.Contains(p.ThreadId))
            .Include(p => p.Author)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        var postLikeCounts = threads.SelectMany(t => t.Posts)
            .ToDictionary(
                p => p.Id,
                p => p.Votes?.Sum(v => v.Value) ?? 0
            );

        List<ThreadDto> threadDtos = new();

        foreach (var thread in threads)
        {
            var threadUserVote = userId != null ? thread.Votes.FirstOrDefault(v => v.UserId == userId)?.Value ?? 0 : 0;
            var threadLikeCount = thread.Votes.Sum(v => v.Value);
            var allPosts = thread.Posts.ToList();
            var topLevel = allPosts.Where(p => p.ParentPostId == null).ToList();
            var replies = allPosts.Where(p => p.ParentPostId != null).ToList();

            var postDtos = topLevel.Select(p =>
            {
                var postUserVote = userId != null ? p.Votes.FirstOrDefault(v => v.UserId == userId)?.Value ?? 0 : 0;
                return new PostDto
                {
                    Id = p.Id,
                    Content = p.Content!,
                    ImageUrl = p.ImageUrl,
                    AuthorUsername = p.Author?.UserName ?? "Unknown",
                    ProfileImageUrl = p.Author?.ProfileImageUrl ?? "https://www.redditstatic.com/avatars/avatar_default_02_FF4500.png",
                    ThreadId = p.ThreadId,
                    ParentPostId = null,
                    CreatedAt = p.CreatedAt,
                    LikeCount = postLikeCounts.GetValueOrDefault(p.Id, 0),
                    UserVote = postUserVote,
                    Replies = BuildReplyTree(replies, postLikeCounts, userId, p.Id)
                };
            }).ToList();

            threadDtos.Add(new ThreadDto
            {
                Id = thread.Id,
                Title = thread.Title,
                Content = thread.Content,
                ImageUrl = thread.ImageUrl,
                ForumId = thread.ForumId,
                ForumTitle = thread.Forum?.Title ?? "Unknown",
                ForumIconUrl = thread.Forum?.IconUrl ?? "Unknown",
                AuthorId = thread.ApplicationUserId,
                AuthorUsername = thread.Author?.UserName ?? "Unknown",
                CreatedAt = thread.CreatedAt,
                LikeCount = threadLikeCount,
                UserVote = threadUserVote,
                PostCount = allPosts.Count,
                Posts = postDtos
            });
        }

        return threadDtos;
    }
    public async Task<List<ThreadDto>> GetThreadsByForumAsync(int forumId, string? userId = null)
    {
        var threads = await _context.Threads
            .Where(t => t.ForumId == forumId)
            .Include(t => t.Author)
            .Include(t => t.Forum)
            .Include(t => t.Votes)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var threadIds = threads.Select(t => t.Id).ToList();

        var posts = await _context.Posts
            .Where(p => threadIds.Contains(p.ThreadId))
            .Include(p => p.Author)
            .Include(p => p.Votes)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        var postLikeCounts = posts.ToDictionary(
            p => p.Id,
            p => p.Votes?.Sum(v => v.Value) ?? 0
        );

        var postsByThread = posts.GroupBy(p => p.ThreadId).ToDictionary(g => g.Key, g => g.ToList());

        List<ThreadDto> threadDtos = new();

        foreach (var thread in threads)
        {
            var threadUserVote = userId != null ? thread.Votes.FirstOrDefault(v => v.UserId == userId)?.Value ?? 0 : 0;
            var threadLikeCount = thread.Votes.Sum(v => v.Value);
            var allPosts = postsByThread.GetValueOrDefault(thread.Id, new List<Post>());
            var topLevel = allPosts.Where(p => p.ParentPostId == null).ToList();
            var replies = allPosts.Where(p => p.ParentPostId != null).ToList();

            var postDtos = topLevel.Select(p =>
            {
                var postUserVote = userId != null
                ? p.Votes.FirstOrDefault(v => v.UserId == userId)?.Value ?? 0
                : 0;
                return new PostDto
                {
                    Id = p.Id,
                    Content = p.Content!,
                    ImageUrl = p.ImageUrl,
                    AuthorUsername = p.Author?.UserName ?? "Unknown",
                    ProfileImageUrl = p.Author?.ProfileImageUrl ?? "https://www.redditstatic.com/avatars/avatar_default_02_FF4500.png",
                    ThreadId = p.ThreadId,
                    ParentPostId = null,
                    CreatedAt = p.CreatedAt,
                    LikeCount = postLikeCounts.GetValueOrDefault(p.Id, 0),
                    UserVote = postUserVote,
                    Replies = BuildReplyTree(replies, postLikeCounts, userId, p.Id)
                };
            }).ToList();

            threadDtos.Add(new ThreadDto
            {
                Id = thread.Id,
                Title = thread.Title,
                Content = thread.Content,
                ImageUrl = thread.ImageUrl,
                ForumId = thread.ForumId,
                ForumTitle = thread.Forum?.Title ?? "Unknown",
                ForumIconUrl = thread.Forum?.IconUrl ?? "Unknown",
                AuthorId = thread.ApplicationUserId,
                AuthorUsername = thread.Author?.UserName ?? "Unknown",
                CreatedAt = thread.CreatedAt,
                LikeCount = threadLikeCount,
                UserVote = threadUserVote,
                PostCount = allPosts.Count,
                Posts = postDtos
            });
        }

        return threadDtos;
    }
    public async Task<List<ThreadDto>> GetSubscribedThreadSummariesAsync(string userId)
    {
        var threads = await _context.Threads
            .Where(t => t.Forum.Users.Any(u => u.Id == userId))
            .Include(t => t.Author)
            .Include(t => t.Forum)
            .Include(t => t.Votes)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        var threadIds = threads.Select(t => t.Id).ToList();

        var posts = await _context.Posts
            .Where(p => threadIds.Contains(p.ThreadId))
            .Include(p => p.Author)
            .Include(p => p.Votes)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        var postLikeCounts = posts.ToDictionary(
            p => p.Id,
            p => p.Votes?.Sum(v => v.Value) ?? 0
        );

        var postsByThread = posts
            .GroupBy(p => p.ThreadId)
            .ToDictionary(g => g.Key, g => g.ToList());


        List<ThreadDto> threadDtos = new();

        foreach (var thread in threads)
        {
            var userVote = thread.Votes.FirstOrDefault(v => v.UserId == userId)?.Value ?? 0;
            var allPosts = postsByThread.GetValueOrDefault(thread.Id, new List<Post>());
            var topLevel = allPosts.Where(p => p.ParentPostId == null).ToList();
            var replies = allPosts.Where(p => p.ParentPostId != null).ToList();

            var postDtos = topLevel.Select(p =>
            {
                var postUserVote = p.Votes.FirstOrDefault(v => v.UserId == userId)?.Value ?? 0;
                return new PostDto
                {
                    Id = p.Id,
                    Content = p.Content!,
                    AuthorUsername = p.Author?.UserName ?? "Unknown",
                    ProfileImageUrl = p.Author?.ProfileImageUrl ?? "https://www.redditstatic.com/avatars/avatar_default_02_FF4500.png",
                    ThreadId = p.ThreadId,
                    ImageUrl = p.ImageUrl,
                    ParentPostId = null,
                    CreatedAt = p.CreatedAt,
                    LikeCount = postLikeCounts.GetValueOrDefault(p.Id, 0),
                    UserVote = postUserVote,
                    Replies = BuildReplyTree(replies, postLikeCounts, userId, p.Id)
                };
            }).ToList();

            threadDtos.Add(new ThreadDto
            {
                Id = thread.Id,
                Title = thread.Title,
                Content = thread.Content,
                ImageUrl = thread.ImageUrl,
                ForumId = thread.ForumId,
                ForumTitle = thread.Forum?.Title ?? "Unknown",
                ForumIconUrl = thread.Forum?.IconUrl ?? "Unknown",
                AuthorId = thread.ApplicationUserId,
                AuthorUsername = thread.Author?.UserName ?? "Unknown",
                CreatedAt = thread.CreatedAt,
                LikeCount = thread.Votes.Sum(v => v.Value),
                UserVote = userVote,
                PostCount = allPosts.Count,
                Posts = postDtos
            });
        }

        return threadDtos;
    }
    private List<PostDto> BuildReplyTree(List<Post> allReplies, Dictionary<int, int> likeCounts, string userId, int? parentId = null)
    {
        return allReplies
            .Where(r => r.ParentPostId == parentId)
            .OrderBy(r => r.CreatedAt)
            .Select(r =>
            {
                var userVote = r.Votes?.FirstOrDefault(v => v.UserId == userId)?.Value ?? 0;
                return new PostDto
                {
                    Id = r.Id,
                    Content = r.Content!,
                    AuthorUsername = r.Author?.UserName ?? "Unknown",
                    ProfileImageUrl = r.Author?.ProfileImageUrl ?? "https://www.redditstatic.com/avatars/avatar_default_02_FF4500.png",
                    ThreadId = r.ThreadId,
                    ImageUrl = r.ImageUrl,
                    ParentPostId = r.ParentPostId,
                    CreatedAt = r.CreatedAt,
                    LikeCount = likeCounts.GetValueOrDefault(r.Id, 0),
                    UserVote = userVote,
                    Replies = BuildReplyTree(allReplies, likeCounts, userId, r.Id)
                };
            }).ToList();
    }

    public async Task<ThreadDto> GetThreadByIdAsync(int threadId, string? viewerUserId = null)
    {
        var thread = await _context.Threads
            .Include(t => t.Author)
            .Include(t => t.Forum)
            .Include(t => t.Votes)
            .Include(t => t.Posts)
            .ThenInclude(p => p.Author)
            .Include(t => t.Posts)
            .ThenInclude(p => p.Votes)
            .FirstOrDefaultAsync(t => t.Id == threadId) ?? throw new KeyNotFoundException("Thread not found");

        var allPosts = thread.Posts.ToList();

        var threadLikeCount = thread.Votes.Sum(v => v.Value);

        var threadUserVote = viewerUserId != null ? thread.Votes.FirstOrDefault(v => v.UserId == viewerUserId)?.Value ?? 0 : 0;

        var postLikeCounts = allPosts.ToDictionary(
            p => p.Id,
            p => p.Votes?.Sum(v => v.Value) ?? 0
        );

        var topLevelPosts = allPosts.Where(p => p.ParentPostId == null).ToList();

        var postsDto = topLevelPosts.Select(p =>
        {
            var userVote = viewerUserId != null ? p.Votes.FirstOrDefault(v => v.UserId == viewerUserId)?.Value ?? 0 : 0;
            return new PostDto
            {
                Id = p.Id,
                Content = p.Content!,
                ImageUrl = p.ImageUrl,
                AuthorUsername = p.Author?.UserName ?? "Unknown",
                ProfileImageUrl = p.Author?.ProfileImageUrl ?? "https://www.redditstatic.com/avatars/avatar_default_02_FF4500.png",
                ThreadId = p.ThreadId,
                ParentPostId = p.ParentPostId,
                CreatedAt = p.CreatedAt,
                LikeCount = postLikeCounts.GetValueOrDefault(p.Id, 0),
                UserVote = userVote,
                Replies = BuildReplyTree(allPosts, postLikeCounts, viewerUserId, p.Id)
            };
        }).ToList();

        return new ThreadDto
        {
            Id = thread.Id,
            Title = thread.Title,
            Content = thread.Content!,
            ImageUrl = thread.ImageUrl,
            ForumId = thread.ForumId,
            ForumTitle = thread.Forum.Title,
            ForumIconUrl = thread.Forum?.IconUrl ?? "Unknown",
            AuthorId = thread.ApplicationUserId!,
            AuthorUsername = thread.Author?.UserName ?? "Unknown",
            PostCount = allPosts.Count,
            LikeCount = threadLikeCount,
            UserVote = threadUserVote,
            CreatedAt = thread.CreatedAt,
            Posts = postsDto
        };
    }

    public async Task<bool> UpdateThreadAsync(int id, string title, string content, string? imageUrl = null)
    {
        var thread = await _context.Threads.FindAsync(id);
        if (thread == null) return false;

        thread.Title = title ?? thread.Title;
        thread.Content = content ?? thread.Content;
        thread.ImageUrl = imageUrl;
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
    public async Task<ThreadDto> VoteThreadAsync(int threadId, string userId, int voteValue)
    {
        if (voteValue is < -1 or > 1)
            throw new ArgumentException("Vote must be -1, 0, or 1.");

        var thread = await _context.Threads
                        .Include(t => t.Votes)
                        .Include(t => t.Posts)
                        .ThenInclude(p => p.Votes)
                        .Include(t => t.Posts)
                        .ThenInclude(p => p.Author)
                        .Include(t => t.Forum)
                        .Include(t => t.Author)
                        .FirstOrDefaultAsync(t => t.Id == threadId) ?? throw new KeyNotFoundException("Thread not found");
        var existingVote = thread.Votes.FirstOrDefault(t => t.UserId == userId);
        var previousValue = existingVote?.Value ?? 0;

        if (existingVote == null && voteValue != 0)
        {
            _context.ThreadVotes.Add(new ThreadVote
            {
                ThreadId = threadId,
                UserId = userId,
                Value = voteValue
            });
        }
        else if (existingVote != null)
        {
            if (voteValue == 0)
                _context.ThreadVotes.Remove(existingVote);
            else
                existingVote.Value = voteValue;
        }
        thread.LikeCount += voteValue - previousValue;
        if (voteValue == 1 && previousValue != 1 && thread.ApplicationUserId != userId)
        {
            await _notificationService.SendNotification(
                   recipientId: thread.ApplicationUserId!,
                   senderId: userId,
                   message: "Your thread was liked!",
                   type: NotificationType.Like,
                   url: $"/threads/{threadId}"
               );
        }
        await _context.SaveChangesAsync();
        var allPosts = thread.Posts.ToList();
        var likeCounts = allPosts.ToDictionary(
            p => p.Id,
            p => p.Votes?.Sum(v => v.Value) ?? 0
        );
        var userVote = thread.Votes.FirstOrDefault(v => v.UserId == userId)?.Value ?? 0;
        List<PostDto> BuildReplyTree(List<Post> posts, int? parentId = null)
        {
            return posts
                .Where(p => p.ParentPostId == parentId)
                .Select(p =>
                {
                    var vote = p.Votes?.FirstOrDefault(v => v.UserId == userId)?.Value ?? 0;
                    return new PostDto
                    {
                        Id = p.Id,
                        Content = p.Content,
                        ImageUrl = p.ImageUrl,
                        CreatedAt = p.CreatedAt,
                        ProfileImageUrl = p.Author?.ProfileImageUrl ?? "https://www.redditstatic.com/avatars/avatar_default_02_FF4500.png",
                        AuthorUsername = p.Author?.UserName ?? "Unknown",
                        ThreadId = p.ThreadId,
                        ParentPostId = p.ParentPostId,
                        LikeCount = likeCounts.GetValueOrDefault(p.Id, 0),
                        UserVote = vote,
                        Replies = BuildReplyTree(posts, p.Id)
                    };
                }).ToList();
        }
        return new ThreadDto
        {
            Id = thread.Id,
            Title = thread.Title,
            Content = thread.Content,
            ImageUrl = thread.ImageUrl,
            ForumId = thread.ForumId,
            ForumTitle = thread.Forum?.Title ?? "",
            ForumIconUrl = thread.Forum?.IconUrl ?? "",
            AuthorId = thread.ApplicationUserId,
            AuthorUsername = thread.Author?.UserName ?? "",
            CreatedAt = thread.CreatedAt,
            LikeCount = thread.LikeCount,
            UserVote = userVote,
            PostCount = thread.Posts?.Count ?? 0,
            Posts = BuildReplyTree(allPosts)
        };

    }
    public async Task<int> GetThreadLikeCountAsync(int threadId)
    {
        return await _context.ThreadVotes
            .CountAsync(tl => tl.ThreadId == threadId);
    }
    public async Task<List<ThreadDto>> SearchThreads(string? viewerUserId = null, string sortBy = "new")
    {
        var threads = await _context.Threads
            .Include(t => t.Author)
            .Include(t => t.Forum)
            .Include(t => t.Posts)
            .Include(t => t.Votes)
            .ToListAsync();

        var result = threads.Select(t =>
        {
            var userVote = viewerUserId != null ? t.Votes.FirstOrDefault(v => v.UserId == viewerUserId)?.Value ?? 0 : 0;
            return new ThreadDto
            {
                Id = t.Id,
                Title = t.Title,
                Content = t.Content,
                ImageUrl = t.ImageUrl,
                AuthorUsername = t.Author?.UserName ?? "Unknown",
                CreatedAt = t.CreatedAt,
                ForumId = t.ForumId,
                ForumTitle = t.Forum?.Title ?? "Unknown",
                ForumIconUrl = t.Forum?.IconUrl ?? "Unknown",
                PostCount = t.Posts.Count,
                LikeCount = t.LikeCount,
                UserVote = userVote,
                Posts = []
            };
        }).ToList();
        var sorted = (sortBy.ToLower() switch
        {
            "best" => result.OrderByDescending(t => t.PostCount),
            "hot" => result.OrderByDescending(t => t.LikeCount),
            "random" => result.OrderBy(_ => Guid.NewGuid()), // This works with in-memory lists
            _ => result.OrderByDescending(t => t.CreatedAt),
        }).ToList();

        return sorted;
    }
    public async Task<List<ThreadDto>> SearchThreadsInForum(int forumId, string? viewerUserId = null, string sortBy = "new")
    {
        var threads = await _context.Threads
                .Where(t => t.ForumId == forumId)
                .Include(t => t.Author)
                .Include(t => t.Posts)
                .Include(t => t.Forum)
                .Include(t => t.Votes)
                .ToListAsync();

        var result = threads.Select(t =>
        {
            var userVote = viewerUserId != null ? t.Votes.FirstOrDefault(v => v.UserId == viewerUserId)?.Value ?? 0 : 0;
            return new ThreadDto
            {
                Id = t.Id,
                Title = t.Title,
                Content = t.Content,
                ImageUrl = t.ImageUrl,
                AuthorUsername = t.Author?.UserName ?? "Unknown",
                CreatedAt = t.CreatedAt,
                ForumId = t.ForumId,
                ForumTitle = t.Forum?.Title ?? "Unknown",
                ForumIconUrl = t.Forum?.IconUrl ?? "Unknown",
                PostCount = t.Posts?.Count ?? 0,
                LikeCount = t.LikeCount,
                UserVote = userVote,
                Posts = []
            };
        }).ToList();
        var sortByLower = sortBy.ToLower();
        List<ThreadDto> sorted;

        switch (sortByLower)
        {
            case "best":
                sorted = result.OrderByDescending(t => t.PostCount).ToList();
                break;
            case "hot":
                sorted = result.OrderByDescending(t => t.LikeCount).ToList();
                break;
            case "popular":
                sorted = result.OrderBy(_ => new Random().Next()).ToList();
                break;
            case "seeded":
                var rand = new Random(12345); // replace with dynamic seed if needed
                sorted = result.OrderBy(_ => rand.Next()).ToList();
                break;
            default:
                sorted = result.OrderByDescending(t => t.CreatedAt).ToList();
                break;
        }

        return sorted;

    }
}