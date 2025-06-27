namespace MyApi.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public string? Token { get; set; }

    public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddDays(7);

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

}