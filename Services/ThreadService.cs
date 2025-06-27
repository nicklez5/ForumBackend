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

    public async Task<Threads> CreateThreadAsync(string title, string content, int forumId, string authorId)
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
        await _context.SaveChangesAsync();
        await _notificationService.CheckForThreadMentionsAsync($"{title} {content}", authorId, thread.Id);
        return thread;
    }
    public async Task<List<ThreadsDto>> GetThreadsByForumAsync(int forumId)
    {
        return await _context.Threads
            .Where(t => t.ForumId == forumId)
            .Include(t => t.Author)
            .Include(t => t.Posts)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new ThreadsDto
            {
                Id = t.Id,
                Title = t.Title!,
                ForumTitle = t.Forum!.Title,
                AuthorUsername = t.Author!.UserName!,
                PostCount = t.Posts!.Count,
                LikeCount = t.Likes!.Count,
                CreatedAt = t.CreatedAt,
            })
            .ToListAsync();
    }
    public async Task<ThreadDto> GetThreadByIdAsync(int id)
    {
        var thread = await _context.Threads
                .Include(t => t.Author)
                .Include(t => t.Forum)
                .Include(t => t.Posts!)
                    .ThenInclude(p => p.Author)
                .Include(t => t.Posts!)
                .ThenInclude(p => p.Replies)
                .ThenInclude(r => r.Author)
                .FirstOrDefaultAsync(t => t.Id == id) ?? throw new KeyNotFoundException("Thread not found");
        var likeCount = await _context.ThreadLikes.CountAsync(tl => tl.ThreadId == id);
        return new ThreadDto
        {
            Id = thread.Id,
            Content = thread.Content,
            Title = thread.Title!,
            CreatedAt = thread.CreatedAt,
            ForumId = thread.ForumId,
            ForumTitle = thread.Forum?.Title,
            AuthorId = thread.ApplicationUserId,
            AuthorUsername = thread.Author?.UserName!,
            PostCount = thread.Posts?.Count ?? 0,
            LikeCount = likeCount,
            Posts = thread.Posts!
                .Where(p => p.ParentPostId == null)
                .Select(p => new PostDto
                {
                    Id = p.Id,
                    Content = p.Content!,
                    AuthorUsername = p.Author?.UserName ?? "Unknown",
                    ThreadId = p.ThreadId,
                    CreatedAt = p.CreatedAt,
                    LikeCount = p.Likes?.Count ?? 0,
                    Replies = p.Replies?
                        .Select(r => new ReplyDto
                        {
                            Id = r.Id,
                            Content = r.Content!,
                            AuthorUsername = r.Author?.UserName ?? "Unknown",
                            CreatedAt = r.CreatedAt
                        }).ToList() ?? new List<ReplyDto>()
                }).ToList()
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
}