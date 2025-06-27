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
    public async Task<IActionResult> CreateThread([FromBody] CreateThreadDto dto)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();
        var thread = await _threadService.CreateThreadAsync(dto.Title, dto.Content, dto.ForumId, userId);
        return Ok(thread);
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> EditThread(int id, [FromBody] EditThreadDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var thread = await _threadService.GetThreadByIdAsync(id);
        if (thread == null)
            return NotFound("Thread not found.");

        if (thread.AuthorUsername != user.UserName)
            return Forbid("You are not allowed to edit this thread.");

        var success = await _threadService.UpdateThreadAsync(id, dto.Title, dto.Content);
        return success ? Ok("Updated") : NotFound("Thread not found");
    }
    [HttpGet("forums/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllThreads(int id)
    {
        var result = await _threadService.GetThreadsByForumAsync(id);
        return Ok(result);
    }
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetThreadById(int id)
    {
        var thread = await _threadService.GetThreadByIdAsync(id);
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
    [HttpPost("{id}/like")]
    public async Task<IActionResult> LikeThread(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        try
        {
            await _threadService.ToggleThreadLikeAsync(id, user.Id);
            return Ok("Thread like toggled.");
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

}