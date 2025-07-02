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
public class PostController(PostService postService, UserManager<ApplicationUser> userManager) : ControllerBase
{
    private readonly PostService _postService = postService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostDto dto)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();
        var post = await _postService.CreatePostAsync(dto.Content, userId, dto.ThreadId,dto.ParentPostId);
        return Ok(post);
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> EditPost(int id, [FromBody] EditPostDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var post = await _postService.GetPostByIdAsync(id);
        if (post == null)
            return NotFound("Post not found.");

        if (post.AuthorUsername != user.UserName)
            return Forbid("You are not allowed to edit this post.");

        var success = await _postService.UpdatePostAsync(id, dto.Content);
        return success ? Ok("Updated") : NotFound("Post not found");
    }
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllPosts()
    {
        var result = await _postService.GetAllPostsAsync();
        return Ok(result);
    }
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPostById(int id)
    {
        var post = await _postService.GetPostByIdAsync(id);
        return Ok(post);
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        var post = await _postService.GetPostByIdAsync(id);
        if (post == null)
            return NotFound("Post not found.");
        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

        if (post.AuthorUsername != user.UserName && !isAdmin)
            return Forbid("You are not allowed to delete this post.");

        var success = await _postService.DeletePostAsync(id);
        return success ? Ok("Deleted") : NotFound("Post not found");
    }
    [HttpPost("{id}/like")]
    public async Task<IActionResult> LikePost(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        try
        {
            await _postService.ToggleLikeAsync(id, user.Id);
            var updatedPost = await _postService.GetPostByIdAsync(id);
            return Ok(updatedPost);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Post not found.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred : {ex.Message}");
        }
    }
    [HttpPost("reply")]
    public async Task<IActionResult> ReplyToPost([FromBody] ReplyPostDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest("Content is required.");

        try
        {
            var result = await _postService.ReplyToPostAsync(dto.ParentPostId, dto.Content, user.Id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }
    [HttpGet("{id}/postLikes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPostLikes(int id)
    {
        var count = await _postService.GetPostLikeCountAsync(id);
        return Ok(count);
    }
    [HttpGet("{id}/postUserLikes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUsersWhoLiked(int id)
    {
        var users = await _postService.GetUsersWhoLikedPostAsync(id);
        return Ok(users);
    }
}