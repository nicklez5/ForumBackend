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
public class ProfileController(UserManager<ApplicationUser> userManager, ProfileService profileService) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ProfileService _profileService = profileService;

    [HttpPut("update")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found.");
        user.FirstName = model.FirstName ?? user.FirstName;
        user.LastName = model.LastName ?? user.LastName;
        user.Bio = model.Bio ?? user.Bio;
        user.ProfileImageUrl = model.ProfileImageUrl ?? user.ProfileImageUrl;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok("Profile updated successfully");
    }
    [HttpPost("upload-profile-image")]
    public async Task<IActionResult> UploadProfileImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot","uploads");
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
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        foreach (var claim in User.Claims)
        {
            Console.WriteLine($"Type: {claim.Type}, Value: {claim.Value}");
        }
        //Console.WriteLine($"USER ID: {userId}");
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found.");

        var profile = new UserProfileDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.UserName,
            Email = user.Email,
            Bio = user.Bio,
            ProfileImageUrl = user.ProfileImageUrl,
            DateJoined = user.DateJoined,
            PostCount = user.PostCount,
            Reputation = user.Reputation,
            IsModerator = user.IsModerator,
            IsBanned = user.IsBanned,
            BannedAt = user.BannedAt
        };
        return Ok(profile);
    }
    [HttpGet("{userId}")]
    public async Task<IActionResult> ViewProfile(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found.");
        var profile = new UserProfileDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.UserName,
            Email = user.Email,
            Bio = user.Bio,
            ProfileImageUrl = user.ProfileImageUrl,
            DateJoined = user.DateJoined,
            PostCount = user.PostCount,
            Reputation = user.Reputation,
            IsModerator = user.IsModerator,
            IsBanned = user.IsBanned,
            BannedAt = user.BannedAt
        };
        return Ok(profile);
    }
    [HttpGet]
    public async Task<IActionResult> ProfileList()
    {
        var profiles = await _profileService.GetAllUsersProfileAsync();
        return Ok(profiles);
    }
}