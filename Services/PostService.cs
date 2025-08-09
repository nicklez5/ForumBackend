using MyApi.Models;
using MyApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Xml.Schema;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;

namespace MyApi.Services;

public class PostService(ApplicationDbContext context, NotificationService notificationService)
{
    private readonly ApplicationDbContext _context = context;
    private readonly NotificationService _notificationService = notificationService;

    public async Task<PostDto> CreatePostAsync(string content, string authorId, int threadId, int? parentPostId = null, string? imageUrl = "")
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
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow
        };
        _context.Posts.Add(post);
        var vote = post.Votes.FirstOrDefault(v => v.UserId == authorId);
        var user = await _context.Users.FindAsync(authorId);
        if (user != null)
        {
            user.PostCount += 1;
            user.Reputation += 5;
        }
        await _context.SaveChangesAsync();
        await CheckForMentions(content ?? "", authorId, post.Id);
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
            ProfileImageUrl = post.Author!.ProfileImageUrl ?? "https://www.redditstatic.com/avatars/avatar_default_02_FF4500.png",
            ThreadId = post.ThreadId,
            CreatedAt = post.CreatedAt,
            ImageUrl = post.ImageUrl,
            ParentPostId = post.ParentPostId,
            LikeCount = 0,
            UserVote = vote?.Value ?? 0,
            Replies = []
        };
    }
    private List<PostDto> BuildReplyTree(List<Post> allReplies, Dictionary<int, int> likeCounts, string userId, int? parentId)
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
                ParentPostId = r.ParentPostId,
                ImageUrl = r.ImageUrl,
                CreatedAt = r.CreatedAt,
                LikeCount = likeCounts.GetValueOrDefault(r.Id, 0),
                UserVote = userVote,
                Replies = BuildReplyTree(allReplies, likeCounts, userId, r.Id)
            };
        })
    
        .ToList();
    }


    public async Task<List<PostDto>> GetPostsByUserAsync(string userId, string? viewerUserId)
    {
        var posts = await _context.Posts
            .Where(p => p.ApplicationUserId == userId)
            .Include(p => p.Thread)
            .ThenInclude(p => p.Author)
            .Include(p => p.Votes)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        var postIds = posts.Select(p => p.Id).ToList();
        var likeCounts = posts.ToDictionary(
            p => p.Id,
            p => p.Votes.Sum(v => v.Value)  // Value = -1, 0, 1
        );

        var replies = posts
            .Where(p => p.ParentPostId != null)
            .ToList();
        var postDtos = posts.Select(post =>
        {
            var userVote =  viewerUserId != null
                ? post.Votes.FirstOrDefault(v => v.UserId == viewerUserId)?.Value ?? 0
            : 0;
            return new PostDto
            {
                Id = post.Id,
                Content = post.Content!,
                AuthorUsername = post.Author?.UserName ?? "Unknown",
                ProfileImageUrl = post.Author?.ProfileImageUrl ?? "https://www.redditstatic.com/avatars/avatar_default_02_FF4500.png",
                ParentPostId = post.ParentPostId,
                ImageUrl = post.ImageUrl,
                ThreadId = post.ThreadId,
                CreatedAt = post.CreatedAt,
                LikeCount = likeCounts.GetValueOrDefault(post.Id, 0),
                UserVote = userVote,
                Replies = []
            };
        }).ToList();
        return postDtos;
    }
    public async Task<List<PostDto>> GetAllPostsAsync(string? userId)
    {
        var posts = await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Votes)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var likeCounts = posts.ToDictionary(
            p => p.Id,
            p => p.Votes.Sum(v => v.Value) // total score: upvotes - downvotes
        );

        // Split replies and top-level posts
        var replies = posts.Where(p => p.ParentPostId != null).ToList();
        var topLevelPosts = posts.Where(p => p.ParentPostId == null).ToList();
        // Now convert top-level posts only and build nested replies
        var postDtos = topLevelPosts.Select(post => {
        var userVote = post.Votes.FirstOrDefault(v => v.UserId == userId)?.Value ?? 0;
            return new PostDto
            {
                Id = post.Id,
                Content = post.Content!,
                AuthorUsername = post.Author?.UserName ?? "Unknown",
                ThreadId = post.ThreadId,
                ParentPostId = post.ParentPostId,
                ProfileImageUrl = post.Author?.ProfileImageUrl ?? "https://www.redditstatic.com/avatars/avatar_default_02_FF4500.png",
                ImageUrl = post.ImageUrl,
                CreatedAt = post.CreatedAt,
                UserVote = userVote,
                LikeCount = likeCounts.GetValueOrDefault(post.Id, 0),
                Replies = BuildReplyTree(replies, likeCounts, post.ApplicationUserId!, post.Id)
            };
        }).ToList();

        return postDtos;
    }
    public async Task<List<PostDto>> GetAllPostsFlat()
    {
        var posts = await _context.Posts
            .AsNoTracking()
            .Select(p => new
            {
                p.Id,
                p.Content,
                p.CreatedAt,
                p.Author,
                p.ImageUrl,
                p.ParentPostId,
                p.ThreadId
            })
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();
        var likeCounts = await _context.PostVotes
            .GroupBy(pl => pl.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count);
        var postsDtos = posts.Select(
            p =>
            {
                return new PostDto
                {
                    Id = p.Id,
                    Content = p.Content!,
                    CreatedAt = p.CreatedAt,
                    AuthorUsername = p.Author.UserName,
                    ImageUrl = p.ImageUrl,
                    LikeCount = likeCounts.GetValueOrDefault(p.Id, 0),
                    ParentPostId = p.ParentPostId,
                    ThreadId = p.ThreadId
                };
            }
        ).ToList();
        return postsDtos;
    }
    public async Task<PostDto> GetPostByIdAsync(int id, string? userId)
    {
        var post = await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Thread)
            .Include(p => p.Votes)
            .FirstOrDefaultAsync(p => p.Id == id) ?? throw new KeyNotFoundException("Post not found");

        var likeCounts = await _context.PostVotes
            .GroupBy(pl => pl.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count);

        var allReplies = await _context.Posts
            .Where(r => r.ThreadId == post.ThreadId && r.ParentPostId != null)
            .Include(r => r.Author)
            .Include(r => r.Votes)
            .ToListAsync();
        var userVote = post.Votes.FirstOrDefault(v => v.UserId == userId)?.Value ?? 0;
        var nestedReplies = BuildReplyTree(allReplies, likeCounts, userId, post.Id);
        return new PostDto
        {
            Id = post.Id,
            Content = post.Content!,
            AuthorUsername = post.Author!.UserName ?? "Unknown",
            ProfileImageUrl = post.Author!.ProfileImageUrl ?? "https://www.redditstatic.com/avatars/avatar_default_02_FF4500.png",
            ThreadId = post.ThreadId,
            ParentPostId = post.ParentPostId,
            ImageUrl = post.ImageUrl,
            CreatedAt = post.CreatedAt,
            LikeCount = likeCounts.GetValueOrDefault(post.Id, 0),
            UserVote = userVote,
            Replies = nestedReplies
        };
    }
    public async Task<bool> UpdatePostAsync(int id, string content, string? imageUrl = null)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post == null) return false;
        
        post.Content = content ?? post.Content;
        post.ImageUrl = imageUrl;
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
    public async Task VotePostAsync(int postId, string userId, int voteValue)
    {
        if (voteValue is < -1 or > 1)
            throw new ArgumentException("Vote must be -1, 0, or 1.");

        var post = await _context.Posts
                    .Include(p => p.Votes)
                    .FirstOrDefaultAsync(p => p.Id == postId) ?? throw new Exception("Post not found");
        var existingVote = post.Votes.FirstOrDefault(v => v.UserId == userId);
        var previousValue = existingVote?.Value ?? 0;

        if (existingVote == null && voteValue != 0)
        {
            _context.PostVotes.Add(new PostVote
            {
                PostId = postId,
                UserId = userId,
                Value = voteValue
            });
        }
        else if (existingVote != null)
        {
            if (voteValue == 0)
                _context.PostVotes.Remove(existingVote);
            else
                existingVote.Value = voteValue;
        }
        post.LikeCount += voteValue - previousValue;
        if (voteValue == 1 && previousValue != 1 && post.ApplicationUserId != userId) {
            await _notificationService.SendNotification(
                recipientId: post.ApplicationUserId!,
                senderId: userId,
                message: "Your post was upvoted!",
                type: NotificationType.Like,
                url: $"/posts/{postId}"
            );
        }
        await _context.SaveChangesAsync();
    }
    public async Task<PostDto> ReplyToPostAsync(int parentPostId, string content, string authorId, string? imageUrl)
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
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow
        };
        _context.Posts.Add(reply);
        var user = await _context.Users.FindAsync(authorId);
        if (user != null)
        {
            user.PostCount += 1;
            user.Reputation += 5;
        }
        await _context.SaveChangesAsync();
        await CheckForMentions(content, authorId, reply.Id);
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
        var userVote = reply.Votes.FirstOrDefault(v => v.UserId == authorId)?.Value ?? 0;
        return new PostDto
        {
            Id = reply.Id,
            AuthorUsername = user?.UserName ?? "Unknown",
            ProfileImageUrl = user?.ProfileImageUrl ?? "https://www.redditstatic.com/avatars/avatar_default_02_FF4500.png",
            ThreadId = parentPost.ThreadId,
            ParentPostId = reply.ParentPostId,
            CreatedAt = reply.CreatedAt,
            ImageUrl = imageUrl,
            LikeCount = 0,
            UserVote = userVote,
            Replies = [],
            Content = content
        };
    }
    public async Task CheckForMentions(string content, string senderId, int postId)
    {
        await _notificationService.CheckForMentionsAsync(content, senderId, postId);
    }
    public async Task<int> GetPostLikeCountAsync(int postId)
    {
        return await _context.PostVotes
            .CountAsync(pl => pl.PostId == postId);
    }
    public async Task<List<string>> GetUsersWhoLikedPostAsync(int postId)
    {
        return await _context.PostVotes
            .Where(pv => pv.PostId == postId)
            .Include(pv => pv.User)
            .Select(pv => pv.User!.UserName!)
            .ToListAsync();
    }

}