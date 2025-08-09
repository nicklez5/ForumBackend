using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyApi.Models;
using MyApi.Services;

namespace MyApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MessageController(IMessageService messageService, UserManager<ApplicationUser> userManager) : ControllerBase
{
    private readonly IMessageService _messageService = messageService;

    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] CreateMessageDto dto)
    {
        var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (senderId == null) return Unauthorized();

        var message = await _messageService.SendMessageAsync(senderId, dto.RecipientId, dto.Content);
        return Ok(message);
    }
    [HttpGet("{contactId}")]
    public async Task<IActionResult> GetMessages(string contactId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return Unauthorized();

        var messages = await _messageService.GetMessagesAsync(currentUserId, contactId);
        return Ok(messages);
    }
    [HttpGet("all")]
    public async Task<IActionResult> GetAllMessages()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return Unauthorized();
        var messages = await _messageService.GetAllUserMessagesAsync(currentUserId);
        return Ok(messages);
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> EditMessage(int id, [FromBody] EditMessageDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var updated = await _messageService.EditMessageAsync(id, userId, dto.Content);
        if (updated == null) return Forbid("Not allowed to edit this message");

        return Ok(new { updated.Id, updated.Content });
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        var success = await _messageService.DeleteMessageAsync(id, userId);
        return success ? Ok("Message deleted.") : Forbid("You can only delete your own message");
    }
}