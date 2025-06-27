using MyApi.Models;
using MyApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Xml.Schema;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;

namespace MyApi.Services;

public class PostService(ApplicationDbContext context, NotificationService notificationService)
{
    private readonly ApplicationDbContext _context = context;
    private readonly NotificationService _notificationService = notificationService;

    public async Task<PostDto> CreatePostAsync(string content, string authorId, int threadId, int? parentPostId = null)
    {
        var thread = await _context.Threads
            .Include(t => t.Author)
            .FirstOrDefaultAsync(t => t.Id == threadId);

        if (thread == null)
            throw new KeyNotFoundException("Thread not found.");
        var post = new Post
        {
            Content = content,
            ApplicationUserId = authorId,
            ThreadId = threadId,
            ParentPostId = parentPostId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        if (parentPostId == null && thread.ApplicationUserId != authorId)
        {
            await _notificationService.SendNotification(
                recipientId: thread.ApplicationUserId!,
                senderId: authorId,
                message: "Someone posted in your thread.",
                type: NotificationType.Reply,
                url: $"/threads/{threadId}"
            );
        }
        if (parentPostId != null)
        {
            var parent = await _context.Posts
                .Where(p => p.Id == parentPostId)
                .Select(p => new { p.ApplicationUserId })
                .FirstOrDefaultAsync();

            if (parent != null && parent.ApplicationUserId != authorId)
            {
                await _notificationService.SendNotification(
                    recipientId: parent.ApplicationUserId!,
                    senderId: authorId,
                    message: "Someone replied to your post.",
                    type: NotificationType.Reply,
                    url: $"/threads/{threadId}"
                );
            }
        }
        return new PostDto
        {
            Id = post.Id,
            Content = content,
            AuthorUsername = post.Author!.UserName ?? "Unknown",
            ThreadId = post.ThreadId,
            CreatedAt = post.CreatedAt,
            LikeCount = 0,
            Replies = []
        };
    }
    public async Task<List<PostDto>> GetAllPostsAsync()
    {
        var posts = await _context.Posts
            .Include(p => p.Author)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var postIds = posts.Select(p => p.Id).ToList();

        var likeCounts = await _context.PostLikes
            .Where(pl => postIds.Contains(pl.PostId))
            .GroupBy(pl => pl.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count);

        var replies = await _context.Posts
            .Where(p => p.ParentPostId != null && postIds.Contains(p.ParentPostId.Value))
            .Include(r => r.Author)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();

        var repliesGrouped = replies
            .GroupBy(r => r.ParentPostId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(r => new ReplyDto
            {
                Id = r.Id,
                Content = r.Content!,
                AuthorUsername = r.Author!.UserName!,
                CreatedAt = r.CreatedAt
            }).ToList());
        
        var postDtos = posts.Select(post => new PostDto
        {
            Id = post.Id,
            Content = post.Content!,
            AuthorUsername = post.Author?.UserName ?? "Unknown",
            ThreadId = post.ThreadId,
            CreatedAt = post.CreatedAt,
            LikeCount = likeCounts.GetValueOrDefault(post.Id, 0),
            Replies = repliesGrouped.GetValueOrDefault(post.Id, new List<ReplyDto>())
        }).ToList();
        return postDtos;
    }
    public async Task<PostDto> GetPostByIdAsync(int id)
    {
        var post = await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Thread)
            .FirstOrDefaultAsync(p => p.Id == id) ?? throw new KeyNotFoundException("Post not found");

        var likeCount = await _context.PostLikes.CountAsync(pl => pl.PostId == id);

        var replies = await _context.Posts
            .Where(r => r.ParentPostId == id)
            .Include(r => r.Author)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new ReplyDto
            {
                Id = r.Id,
                Content = r.Content!,
                AuthorUsername = r.Author!.UserName!,
                CreatedAt = r.CreatedAt
            }).ToListAsync();

        return new PostDto
        {
            Id = post.Id,
            Content = post.Content!,
            AuthorUsername = post.Author!.UserName ?? "Unknown",
            ThreadId = post.ThreadId,
            CreatedAt = post.CreatedAt,
            LikeCount = likeCount,
            Replies = replies
        };
    }
    public async Task<bool> UpdatePostAsync(int id, string content)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post == null) return false;
        post.Content = content ?? post.Content;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePostAsync(int id)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post == null) return false;
        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
        return true;
    }
    public async Task ToggleLikeAsync(int postId, string userId)
    {
        var existingLike = await _context.PostLikes
            .FirstOrDefaultAsync(pl => pl.PostId == postId && pl.ApplicationUserId == userId);

        if (existingLike != null)
        {
            _context.PostLikes.Remove(existingLike);
        }
        else
        {
            var like = new PostLike
            {
                PostId = postId,
                ApplicationUserId = userId
            };
            _context.PostLikes.Add(like);
            var post = await _context.Posts.Include(p => p.Author).FirstOrDefaultAsync(p => p.Id == postId);
            if (post != null && post.ApplicationUserId != userId)
            {
                await _notificationService.SendNotification(
                    recipientId: post.ApplicationUserId!,
                    senderId: userId,
                    message: "Your post was liked!",
                    type: NotificationType.Like,
                    url: $"/posts/{postId}"
                );
            }
        }
        await _context.SaveChangesAsync();
    }
    public async Task<ReplyPostDto> ReplyToPostAsync(int parentPostId, string content, string authorId)
    {
        var parentPost = await _context.Posts
            .Include(p => p.Thread)
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == parentPostId) ?? throw new KeyNotFoundException("Parent post not found");
        var reply = new Post
        {
            Content = content,
            ApplicationUserId = authorId,
            ThreadId = parentPost.ThreadId,
            ParentPostId = parentPostId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Posts.Add(reply);
        await _context.SaveChangesAsync();

        if (parentPost.ApplicationUserId != authorId)
        {
            await _notificationService.SendNotification(
                recipientId: parentPost.ApplicationUserId!,
                senderId : authorId,
                message: "Someone replied to your post.",
                type: NotificationType.Reply,
                url: $"/threads/{parentPost.ThreadId}"
            );
        }
        return new ReplyPostDto
        {
            threadId = parentPost.ThreadId,
            parentPostId = parentPostId,
            Content = content
        };
    }
    public async Task CheckForMentions(string content, string senderId, int postId)
    {
        await _notificationService.CheckForMentionsAsync(content, senderId, postId);
    }
    public async Task<int> GetPostLikeCountAsync(int postId)
    {
        return await _context.PostLikes
            .CountAsync(pl => pl.PostId == postId);
    }
    public async Task<List<string>> GetUsersWhoLikedPostAsync(int postId)
    {
        return await _context.PostLikes
            .Where(pl => pl.PostId == postId)
            .Include(pl => pl.User)
            .Select(pl => pl.User!.UserName!)
            .ToListAsync();
    }

}