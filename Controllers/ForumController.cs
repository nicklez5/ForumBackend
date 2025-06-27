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
public class ForumController(ForumService forumService) : ControllerBase
{
    private readonly ForumService _forumService = forumService;

    [HttpPost]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> CreateForum([FromBody] CreateForumDto dto)
    {
        var forum = await _forumService.CreateForumAsync(dto.Title, dto.Description!, dto.ImageUrl);
        return Ok(forum);
    }
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> EditForum(int id, [FromBody] EditForumDto dto)
    {
        var success = await _forumService.UpdateForumAsync(id, dto.Title!, dto.Description!, dto.ImageUrl);
        return success ? Ok("Updated") : NotFound("Forum not found");
    }
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> DeleteForum(int id)
    {
        var success = await _forumService.DeleteForumAsync(id);
        return success ? Ok("Deleted") : NotFound("Forum not found");
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllForums()
    {
        var forums = await _forumService.GetAllForumsAsync();
        return Ok(forums);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetForumById(int id)
    {
        var forum = await _forumService.GetForumByIdAsync(id);
        return Ok(forum);
    }
}