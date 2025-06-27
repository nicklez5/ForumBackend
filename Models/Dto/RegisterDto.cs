using System.ComponentModel.DataAnnotations;
namespace MyApi.Models;
public class RegisterDto
{
    [Required]
    public required string Username { get; set; }
    [Required]
    public required string Email { get; set; }

    [Required]
    public required string FirstName { get; set; }

    [Required]
    public required string LastName { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least 2 characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password",ErrorMessage = "The password and confirmation password do not match.")]
    public string? ConfirmPassword { get; set; }
}