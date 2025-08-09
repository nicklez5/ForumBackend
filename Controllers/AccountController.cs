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
public class AccountController(UserManager<ApplicationUser> userManager, ThreadService threadService, PostService postService, ForumService forumService) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    private readonly ThreadService _threadService = threadService;
    private readonly PostService _postService = postService;

    private readonly ForumService _forumService = forumService;

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
    [HttpPost("change-username/{userId}")]
    public async Task<IActionResult> ChangeUsername(string userId, [FromBody] ChangeUsernameDto model)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");
        else
        {
            var usernameResult = await _userManager.SetUserNameAsync(user, model.NewUsername);
            if (usernameResult.Succeeded)
            {
                return Ok("Username changed successfully to " + model.NewUsername);
            }
            else
            {
                return BadRequest(usernameResult.Errors);
            }
        }
    }
    [HttpPost("change-email/{userId}")]
    public async Task<IActionResult> ChangeEmail(string userId, [FromBody] ChangeEmailDto model)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");
        else
        {
            var emailResult = await _userManager.SetEmailAsync(user, model.NewEmail);
            if (emailResult.Succeeded)
            {
                return Ok("Email changed successfully to " + model.NewEmail);
            }
            else
            {
                return BadRequest(emailResult.Errors);
            }
        }
    }
    [HttpGet("activity/{userId}")]
    public async Task<IActionResult> GetUserActivity(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");
        var viewerUserId = User.Identity?.IsAuthenticated == true
        ? User.FindFirstValue(ClaimTypes.NameIdentifier)
        : null;
        var posts = await _postService.GetPostsByUserAsync(userId,viewerUserId);

        var threads = await _threadService.GetThreadsByUserAsync(userId);

        var forums = await _forumService.GetAllForumsAsync();
        var createdForums = await _forumService.GetForumsCreatedByUserAsync(user.UserName);
        var totalSubscribedForumCount = forums.Count(f => f.Users.Any(u => u.Username == user.UserName));
        var totalPostLikeCount = posts.Sum(p => p.LikeCount);
        var totalThreadLikeCount = threads.Sum(t => t.LikeCount);
        return Ok(new UserActivityDto { Posts = posts, Threads = threads, Forums = createdForums,TotalPostLikeCount = totalPostLikeCount, TotalThreadLikeCount = totalThreadLikeCount, TotalSubscribedForumCount = totalSubscribedForumCount });
    }
    [HttpGet("subscribed/{userId}")]
    public async Task<IActionResult> GetUserSubscribedActivity(string userId)
    {
        var forums = await _forumService.GetForumsByUserIdAsync(userId);
        var threads = await _threadService.GetSubscribedThreadSummariesAsync(userId);
        return Ok(new UserSubscribedDto { forums = forums, threads = threads });
    }
}