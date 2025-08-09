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
public class AdminController(UserManager<ApplicationUser> userManager, NotificationService notificationService, ApplicationDbContext context) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly NotificationService _notificationService = notificationService;
    private readonly ApplicationDbContext _context = context;
    [HttpPost("promote")]
    public async Task<IActionResult> PromoteToAdmin([FromQuery] string Username)
    {
        return await MakeAdmin(Username);
    }
    private async Task<IActionResult> MakeAdmin(string Username)
    {
        var user = await _userManager.FindByNameAsync(Username);
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
        if (string.IsNullOrWhiteSpace(dto.Username))
            return BadRequest("Username is required");

        var user = await _userManager.FindByNameAsync(dto.Username);
        if (user == null)
            return NotFound($"No user found with Username: {dto.Username}");

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
    public async Task<IActionResult> BanUser([FromQuery] string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest("Username is required.");
        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
            return NotFound($"No user found with username: {username}");
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
    public async Task<IActionResult> UnbanUser([FromQuery] string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest("Username is required");
        }
        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
            return NotFound($"User with username {username} not found.");
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
    [HttpGet]
    public async Task<IActionResult> fetchAllUsers()
    {
        var allUsers = _userManager.Users.Select(u => new
        {
            Id = u.Id,
            Username = u.UserName,
            Email = u.Email,
            Banned = u.IsBanned,
            Admin = u.IsModerator,
        });
        return Ok(await allUsers.ToListAsync());
    }
    [HttpPost("isAdmin")]
    public async Task<IActionResult> IsUserAdmin([FromBody] SystemEmail dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.identifier))
            return BadRequest("Invalid identifier.");

        var user = await _userManager.FindByNameAsync(dto.identifier);
        if (user == null)
            user = await _userManager.FindByEmailAsync(dto.identifier);

        if (user == null)
            return NotFound("User not found.");

        var result = user.IsModerator == true;
        return Ok(result);
    }
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = new
        {
            UsersCount = await _userManager.Users.CountAsync(),
            ForumsCount = await _context.Forums.CountAsync(),
            ThreadsCount = await _context.Threads.CountAsync(),
            PostsCount = await _context.Posts.CountAsync(),
            MessagesCount = await _context.Messages.CountAsync()
        };
        return Ok(stats);
    }
    public class SystemEmail
    {
        public string? identifier { get; set; } = string.Empty;
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