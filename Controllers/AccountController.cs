using System.ComponentModel;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MyApi.Models;
using MyApi.Services;
namespace MyApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AccountController(UserManager<ApplicationUser> userManager, ThreadService threadService, PostService postService) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    private readonly ThreadService _threadService = threadService;
    private readonly PostService _postService = postService;

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found.");
        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok("Password changed successfully.");
    }
    [HttpGet("activity/{userId}")]
    public async Task<IActionResult> GetUserActivity(string userId)
    {
        var posts = await _postService.GetPostsByUserAsync(userId);
        var threads = await _threadService.GetThreadsByUserAsync(userId);
        return Ok(new UserActivityDto { Posts = posts, Threads = threads });
    }
}