using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyApi.Models;
namespace MyApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProfileController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

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
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.UserName,
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
    [HttpGet("{id}")]
    public async Task<IActionResult> ViewProfile(int id)
    {
        var userId = id.ToString();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found.");
        var profile = new UserProfileDto
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.UserName,
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
}