using MyApi.Models;
using MyApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Xml.Schema;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;

namespace MyApi.Services;

public class ProfileService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<List<UserProfileDto>> GetAllUsersProfileAsync()
    {
        return await _context.Users
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Username = u.UserName,
                Bio = u.Bio,
                ProfileImageUrl = u.ProfileImageUrl,
                DateJoined = u.DateJoined,
                PostCount = u.PostCount,
                Reputation = u.Reputation,
                IsModerator = u.IsModerator,
                IsBanned = u.IsBanned
            }).ToListAsync();
    }
}