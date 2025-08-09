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
public class ForumController(UserManager<ApplicationUser> userManager,ForumService forumService) : ControllerBase
{
    private readonly ForumService _forumService = forumService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    [HttpPost]
    public async Task<IActionResult> CreateForum([FromForm] CreateForumDto dto)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();
        string? iconUrl = null;
        string? bannerUrl = null;
        if (dto.Icon != null && dto.Icon.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "icon", "forums");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Icon.FileName);
            var filePath = Path.Combine("wwwroot/icon/forums", fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.Icon.CopyToAsync(stream);
            }
            iconUrl = "/icon/forums/" + fileName;
        }
        if (dto.Banner != null && dto.Banner.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "banner", "forums");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Banner.FileName);
            var filePath = Path.Combine("wwwroot/banner/forums", fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.Banner.CopyToAsync(stream);
            }
            bannerUrl = "/banner/forums/" + fileName;
        }
        var forum = await _forumService.CreateForumAsync(dto.Title, dto.Description!, iconUrl,bannerUrl, userId);
        return Ok(forum);
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> EditForum(int id, [FromForm] EditForumDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        var forum = await _forumService.GetForumByIdAsync(id);
        if(forum == null)
            return NotFound("Forum not found.");

        if (forum.Author != user.UserName && !await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return Unauthorized("You are not the admin of this forum");
        }
        string? iconUrl = forum.IconUrl;
        string? bannerUrl = forum.BannerUrl;
        if (dto.RemoveIcon)
        {
            if (!string.IsNullOrEmpty(iconUrl))
            {
                var fullPath = Path.Combine("wwwroot", iconUrl.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
                iconUrl = null;
            }
        }
        if (dto.RemoveBanner)
        {
            if (!string.IsNullOrEmpty(bannerUrl))
            {
                var fullPath = Path.Combine("wwwroot", bannerUrl.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
                bannerUrl = null;
            }
        }
        if (dto.Banner is { Length: > 0 })
        {
            var uploadsFolder = Path.Combine("wwwroot", "banner", "forums");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);
            var ext = Path.GetExtension(dto.Banner.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowedExtensions.Contains(ext))
                return BadRequest("Invalid image file type.");
            var bannerFileName = Guid.NewGuid().ToString() + ext;
            var bannerPath = Path.Combine(uploadsFolder, bannerFileName);

            using (var stream = new FileStream(bannerPath, FileMode.Create))
            {
                await dto.Banner.CopyToAsync(stream);
            }
            bannerUrl = "/banner/forums/" + bannerFileName;
        }
        if (dto.Icon is { Length: > 0 })
        {
            var uploadsFolder = Path.Combine("wwwroot", "icon", "forums");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);
            var ext = Path.GetExtension(dto.Icon.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowedExtensions.Contains(ext))
                return BadRequest("Invalid image file type.");
            var iconFileName = Guid.NewGuid().ToString() + ext;
            var iconPath = Path.Combine(uploadsFolder, iconFileName);

            using (var stream = new FileStream(iconPath, FileMode.Create))
            {
                await dto.Icon.CopyToAsync(stream);
            }
            iconUrl = "/icon/forums/" + iconFileName;
        }
        var success = await _forumService.UpdateForumAsync(id, dto.Title, dto.Description, bannerUrl, iconUrl, dto.RemoveBanner, dto.RemoveIcon);
        return success ? Ok(await _forumService.GetForumByIdAsync(id)) : NotFound("Forum not found");
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteForum(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        var forum = await _forumService.GetForumByIdAsync(id);
        if (forum == null)
            return NotFound("Forum not found");

        if (forum.Author != user.UserName && !await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return Unauthorized("You are not allowed to delete this forum");
        }
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
    [HttpPost("{id}/subscribe")]
    public async Task<IActionResult> Subscribe(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var success = await _forumService.SubscribeUserToForumAsync(user.Id, id);
        return success ? Ok("Subscribed.") : BadRequest("Could not subscribe.");
        
    }
    [HttpPost("upload-forum-image")]
    public async Task<IActionResult> UploadForumImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");
        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);
        var filePath = Path.Combine(uploadsFolder, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        var fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";
        return Ok(new { url = fileUrl });
    }
    [HttpPost("{id}/unsubscribe")]
    public async Task<IActionResult> Unsubscribe(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var success = await _forumService.UnSubscribeUserToForumAsync(user.Id, id);
        return success ? Ok("Unsubscribed.") : BadRequest("Could not unsubscribed.");
    }
    [HttpGet("{id}/subscribed")]
    public async Task<IActionResult> Subscribed(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        var success = await _forumService.IsUserSubscribedAsync(user.Id, id);
        return Ok(success);
    }
    [HttpGet("my-subscribed-forums")]
    public async Task<IActionResult> GetMySubscribedForums()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var forums = await _forumService.GetForumsByUserIdAsync(user.Id);
        return Ok(forums);
    }
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        var result = await _forumService.SearchContentAsync(query);
        return Ok(result);
    }
}