using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Models;
using MyApi.Services;

namespace MyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireAdminRole")]
public class AdminController(UserManager<ApplicationUser> userManager,NotificationService notificationService) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly NotificationService _notificationService = notificationService;
    [HttpPost("promote")]
    public async Task<IActionResult> PromoteToAdmin([FromQuery] string email)
    {
        return await MakeAdmin(email);
    }
    private async Task<IActionResult> MakeAdmin(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return NotFound("User not found");
        if (await _userManager.IsInRoleAsync(user, "Admin"))
            return Ok("Already admin");
        var result = await _userManager.AddToRoleAsync(user, "Admin");
        user.IsModerator = true;

        await _userManager.UpdateAsync(user);

        var currentUser = await _userManager.GetUserAsync(User);
        var message = $"Your are granted admin privileges";

        await _notificationService.SendNotification(user.Id, currentUser!.Id, message, NotificationType.ModeratorAction);
        return result.Succeeded ? Ok("Promoted to admin") : BadRequest(result.Errors);
    }
    [HttpPost("unadmin")]
    public async Task<IActionResult> RevokeAdmin([FromBody] RevokeAdminDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest("Email is required");

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return NotFound($"No user found with email: {dto.Email}");

        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (!isAdmin)
            return BadRequest("User is not an admin");

        var result = await _userManager.RemoveFromRoleAsync(user, "Admin");
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }
        user.IsModerator = false;
        await _userManager.UpdateAsync(user);

        var currentUser = await _userManager.GetUserAsync(User);
        var message = $"Your admin privileges have been revoked";

        await _notificationService.SendNotification(user.Id, currentUser!.Id, message, NotificationType.ModeratorAction);
        return Ok(new
        {
            success = true,
            message = $"User {user.UserName} has been removed from the Admin role."
        });
    }
    [HttpPost("ban")]
    public async Task<IActionResult> BanUser([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Email is required.");
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return NotFound($"No user found with email: {email}");
        user.IsBanned = true;
        user.BannedAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);
        var currentUser = await _userManager.GetUserAsync(User);

        var message = $"You are banned at {user.BannedAt:yyyy-MM-dd HH:mm:ss} UTC";
        await _notificationService.SendNotification(user.Id, currentUser!.Id, message, NotificationType.ModeratorAction);

        return Ok(new
        {
            success = true,
            message = $"User {user.UserName} was banned at {user.BannedAt:yyyy-MM-dd HH:mm:ss} UTC"
        });
    }
    [HttpPost("unban")]
    public async Task<IActionResult> UnbanUser([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Email is required");
        }
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return NotFound($"User with email {email} not found.");
        if (!user.IsBanned)
        {
            return Ok("User is not currently banned.");
        }
        user.IsBanned = false;
        user.BannedAt = null;

        await _userManager.UpdateAsync(user);

        var currentUser = await _userManager.GetUserAsync(User);
        var message = $"You are unbanned at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
        await _notificationService.SendNotification(user.Id, currentUser!.Id, message, NotificationType.ModeratorAction);

        return Ok(new
        {
            success = true,
            message = $"User {user.UserName} has been unbanned"
        });
    }
    [HttpPost("alert")]
    public async Task<IActionResult> SendSystemAlert([FromBody] SystemAlertDto dto)
    {
        var allUsers = await _userManager.Users.ToListAsync();
        var currentUser = await _userManager.GetUserAsync(User);
        foreach (var user in allUsers)
        {
            await _notificationService.SendNotification(user.Id, currentUser!.Id, dto.Message, NotificationType.SystemAlert);
        }
        return Ok("System alert sent to all users.");
    }
    // [HttpDelete("delete-thread/{id}")]
    // public async Task<IActionResult> DeleteThread(int id)
    // {
    //     var thread = await _context.Threads.FindAsync(id);
    //     if (thread == null)
    //         return NotFound("Thread not found");
    //     _context.Threads.Remove(thread);
    //     await _context.SaveChangesAsync();

    //     return Ok(new { message = $"Thread {id} has been deleted." });
    // }
    // [HttpDelete("delete-post/{id}")]
    // public async Task<IActionResult> DeletePost(int id)
    // {
    //     var post = await _context.Posts.FindAsync(id);
    //     if (post == null)
    //         return NotFound("Post not found");
    //     _context.Posts.Remove(post);
    //     await _context.SaveChangesAsync();

    //     return Ok(new {message = $"Post {id} has been deleted."});
    // }
}