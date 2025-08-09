using System;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MyApi.Data;
using MyApi.Models;
using MyApi.Services;

namespace MyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IEmailService emailService,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IConfiguration configuration,
    ApplicationDbContext context
    ) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;

    private readonly IConfiguration _configuration = configuration;

    private readonly IEmailService _emailService = emailService;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        var user = new ApplicationUser
        {
            UserName = model.Username,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName
        };

        var result = await _userManager.CreateAsync(user, model.Password!);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");
            return Ok(new { message = "Registration successful" });
        }
        return BadRequest(result.Errors);
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Identifier))
            return BadRequest("Username or email is required");
        var user = await _userManager.FindByEmailAsync(model.Identifier);
        user ??= await _userManager.FindByNameAsync(model.Identifier);
        if (user == null)
            return Unauthorized("User not found.");
        var result = await _signInManager.CheckPasswordSignInAsync(
            user, model.Password, lockoutOnFailure: false
        );
        if (result.Succeeded)
        {
            if (user.IsBanned)
            {
                return BadRequest($"You are banned since {user.BannedAt:yyyy-MM-dd HH:mm:ss} UTC");
            }
            else
            {
                var NewAccessToken = await GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                user.RefreshTokens.Add(refreshToken);
                await _userManager.UpdateAsync(user);

                return Ok(new
                {
                    accessToken = NewAccessToken,
                    refreshToken = refreshToken.Token
                });
                            
            }
        }
        return BadRequest("Invalid credentials");
    }
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        var tokenEntry = await _context.RefreshTokens
            .Include(t => t.User)
            .Where(t => t.Token == refreshTokenDto.Token && t.ExpiryDate > DateTime.UtcNow)
            .SingleOrDefaultAsync();

        var user = tokenEntry?.User;
            
        
        if (user == null)
            return BadRequest("Invalid token");

        var newAccessToken = await GenerateJwtToken(user);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshTokens.RemoveAll(t => t.Token == refreshTokenDto.Token);
        user.RefreshTokens.Add(newRefreshToken);
        await _userManager.UpdateAsync(user);

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken.Token
        });
    }
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var user = await _userManager.FindByNameAsync(dto.Username!);
        if (user == null)
        {
            return Ok("If an account with that username exists, a reset link has been sent.");
        }
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var encodedToken = WebEncoders.Base64UrlEncode(tokenBytes);
        var resetLink = $"{dto.ClientUrl}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={encodedToken}";
        await _emailService.SendEmailAsync(user.Email!, "Reset Your Password", $"Click to reset: {resetLink}");
        return Ok("If an account with that email exists, a reset link has been sent.");
    }
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email!);
        if (user == null)
        {
            return BadRequest("Invalid request");
        }
        var tokenBytes = WebEncoders.Base64UrlDecode(dto.Token!);
        var decodedToken = Encoding.UTF8.GetString(tokenBytes);
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, dto.NewPassword!);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok("Password reset successful");
    }
    private static RefreshToken GenerateRefreshToken()
    {
        return new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiryDate = DateTime.UtcNow.AddDays(7)
        };
    }
    private async Task<string> GenerateJwtToken(ApplicationUser user)
    {
        var userRoles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim> {
            new(JwtRegisteredClaimNames.Sub, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!)
        };
        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(3),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    public class RefreshTokenDto
    {
        [Required]
        public string? Token { get; set; }
    }
}