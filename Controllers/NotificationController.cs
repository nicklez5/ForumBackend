using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Models;
using MyApi.Services;
namespace MyApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationController(NotificationService notificationService, UserManager<ApplicationUser> userManager) : ControllerBase
{
    private readonly NotificationService _notificationService = notificationService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var notifications = await _notificationService.GetAllNotifications(user.Id);
        return Ok(notifications);
    }
    [HttpPatch("{id}")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var success = await _notificationService.MarkasRead(id);
        return success ? Ok("Marked as Read") : NotFound("Notification not found");
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var success = await _notificationService.DeleteNotificationsAsync(id);
        return success ? Ok("Notification deleted") : NotFound("Notification not found");
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetNotificationById(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        var notification = await _notificationService.GetNotificationByIdAsync(id, user.Id);
        return Ok(notification);
    }
    [HttpPost]
    public async Task<IActionResult> SendNotification([FromBody] NotificationDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        if (!Enum.TryParse<NotificationType>(dto.Type, out var typeEnum))
            return BadRequest("Invalid notification type");
        await _notificationService.SendNotification(dto.recipientId, user.Id, dto.message, typeEnum, dto.url);
        return Ok("Notification sent");
    }
}