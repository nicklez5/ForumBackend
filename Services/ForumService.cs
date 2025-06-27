using MyApi.Models;
using MyApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Xml.Schema;

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
            Title = f.Title,
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
}