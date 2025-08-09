using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
    public async Task<IActionResult> CreatePost([FromForm] CreatePostDto dto)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        string? imagePath = null;
        if (dto.Image != null && dto.Image.Length > 0)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Image.FileName);
            var filePath = Path.Combine("wwwroot/images/posts", fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.Image.CopyToAsync(stream);
            }
            imagePath = "/images/posts/" + fileName;
        }
        var post = await _postService.CreatePostAsync(dto.Content, userId, dto.ThreadId,dto.ParentPostId, imagePath);
        return Ok(post);
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> EditPost(int id, [FromForm] EditPostDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var post = await _postService.GetPostByIdAsync(id, user.Id);
        if (post == null)
            return NotFound("Post not found.");

        if (post.AuthorUsername != user.UserName && !await _userManager.IsInRoleAsync(user,"Admin"))
            return Forbid("You are not allowed to edit this post.");

        string? imageUrl = post.ImageUrl;

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
            var uploadsFolder = Path.Combine("wwwroot", "images", "posts");
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

            imageUrl = "/images/posts/" + fileName;
        }

        var success = await _postService.UpdatePostAsync(id, dto.Content, imageUrl);
        return success
            ? Ok(await _postService.GetPostByIdAsync(id, user.Id))
            : NotFound("Post not found");
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllPosts()
    {
        var userId = User.Identity?.IsAuthenticated == true
        ? User.FindFirstValue(ClaimTypes.NameIdentifier)
        : null;
        var result = await _postService.GetAllPostsAsync(userId);
        return Ok(result);
    }
    [HttpGet("posts")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllPostsFlat()
    {
        var posts = await _postService.GetAllPostsFlat();
        return Ok(posts);
    }
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPostById(int id)
    {
        var userId = User.Identity?.IsAuthenticated == true
        ? User.FindFirstValue(ClaimTypes.NameIdentifier)
        : null;
        var post = await _postService.GetPostByIdAsync(id,userId);
        return Ok(post);
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        var post = await _postService.GetPostByIdAsync(id,user.Id);
        if (post == null)
            return NotFound("Post not found.");
        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

        if (post.AuthorUsername != user.UserName && !isAdmin)
            return Forbid("You are not allowed to delete this post.");

        var success = await _postService.DeletePostAsync(id);
        return success ? Ok("Deleted") : NotFound("Post not found");
    }
    [HttpPost("vote")]
    public async Task<IActionResult> Vote([FromBody] PostVoteDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        try
        {
            await _postService.VotePostAsync(dto.PostId, userId, dto.Vote);
            var updatedPost = await _postService.GetPostByIdAsync(dto.PostId,userId);
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
    public async Task<IActionResult> ReplyToPost([FromForm] ReplyPostDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        if (string.IsNullOrEmpty(dto.Content) && dto.Image == null) {
            return BadRequest("Either content or image is required.");
        }
        string? imagePath = null;
        if (dto.Image != null && dto.Image.Length > 0)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Image.FileName);
            var filePath = Path.Combine("wwwroot/images", fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.Image.CopyToAsync(stream);
            }
            imagePath = "/images/" + fileName;
        }
        try
        {

            var result = await _postService.ReplyToPostAsync(dto.ParentPostId, dto.Content, user.Id, imagePath);
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