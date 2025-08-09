using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Models;
using MyApi.Services;
using SQLitePCL;

namespace MyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ThreadController(ThreadService threadService, UserManager<ApplicationUser> userManager) : ControllerBase
{
    private readonly ThreadService _threadService = threadService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateThread([FromForm] CreateThreadDto dto)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();
        string? imageUrl = null;
        if (dto.Image != null && dto.Image.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "threads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Image.FileName);
            var filePath = Path.Combine("wwwroot/images/threads", fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.Image.CopyToAsync(stream);
            }
            imageUrl = "/images/threads/" + fileName;
        }
        var thread = await _threadService.CreateThreadAsync(dto.Title, dto.Content, dto.ForumId, userId, imageUrl);
        return Ok(thread);
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> EditThread(int id, [FromForm] EditThreadDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var thread = await _threadService.GetThreadByIdAsync(id);
        if (thread == null)
            return NotFound("Thread not found.");

        if (thread.AuthorUsername != user.UserName && !await _userManager.IsInRoleAsync(user,"Admin"))
            return Forbid("You are not allowed to edit this thread.");

        string? imageUrl = thread.ImageUrl;

        if (dto.RemoveImage)
        {
            if (!string.IsNullOrEmpty(imageUrl))
            {
                var fullPath = Path.Combine("wwwroot", imageUrl.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }
            imageUrl = null;
        }
        else if (dto.Image is { Length: > 0 })
        {
            var uploadsFolder = Path.Combine("wwwroot", "images", "threads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);
            var ext = Path.GetExtension(dto.Image.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            if (!allowedExtensions.Contains(ext))
                return BadRequest("Invalid image file type.");
            var fileName = Guid.NewGuid().ToString() + ext;
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.Image.CopyToAsync(stream);
            }
            imageUrl = "/images/threads/" + fileName;
        }
        var success = await _threadService.UpdateThreadAsync(id, dto.Title, dto.Content, imageUrl);
        return success ? Ok(await _threadService.GetThreadByIdAsync(id, user.Id)) : NotFound("Thread not found");
    }
    [HttpGet("all")]
    public async Task<IActionResult> GetAllThread([FromQuery] string? viewerUserId = null)
    {
        var result = await _threadService.GetAllThreads(viewerUserId);
        return Ok(result);
    }
    [HttpGet("forums/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllThreads(int id, [FromQuery] string? viewerUserId = null)
    {
        var result = await _threadService.GetThreadsByForumAsync(id, viewerUserId);
        return Ok(result);
    }
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetThreadById(int id, [FromQuery] string? viewerUserId)
    {
        var thread = await _threadService.GetThreadByIdAsync(id, viewerUserId);
        return Ok(thread);
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteThread(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        var thread = await _threadService.GetThreadByIdAsync(id);
        if (thread == null)
            return NotFound("Thread not found.");
        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (thread.AuthorUsername != user.UserName && !isAdmin)
            return Forbid("You are not allowed to delete this thread.");

        var success = await _threadService.DeleteThreadAsync(id);
        return success ? Ok("Deleted") : NotFound("Thread not found");
    }
    [HttpPost("vote")]
    public async Task<IActionResult> LikeThread([FromBody] ThreadVoteDto voteDto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        try
        {
            var updatedThread = await _threadService.VoteThreadAsync(voteDto.ThreadId, user.Id, voteDto.Vote);
            return Ok(updatedThread);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Thread not found.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }
    [HttpGet("{id}/likes")]
    public async Task<IActionResult> GetThreadLikes(int id)
    {
        var count = await _threadService.GetThreadLikeCountAsync(id);
        return Ok(count);
    }
    [HttpGet("search")]
    public async Task<IActionResult> SearchThreads([FromQuery] string? viewerUserId = null, [FromQuery] string sortBy = "new")
    {
        var results = await _threadService.SearchThreads(viewerUserId, sortBy);
        return Ok(results);
    }
    [HttpGet("{id}/search")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchThreadsByForum(int id, [FromQuery] string? viewerUserId = null, [FromQuery] string sortBy = "new")
    {
        var results = await _threadService.SearchThreadsInForum(id, viewerUserId, sortBy);
        return Ok(results);
    }
}