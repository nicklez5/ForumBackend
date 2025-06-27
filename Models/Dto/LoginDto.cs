using System.ComponentModel.DataAnnotations;
namespace MyApi.Models;
public class LoginDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least 2 characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    public required string Password { get; set; }
}