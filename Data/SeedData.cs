using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyApi.Models;

namespace MyApi.Data;

public static class SeedData
{
    public static async Task SeedAdminUser(UserManager<ApplicationUser> userManager)
    {
        
        string adminEmail = "jackson2k11@gmail.com";
        string adminUsername = "jackson2k11";
        string adminFirstName = "Jackson";
        string adminLastName = "Lu";
        string adminPassword = "Admin@123";
        string adminRole = "Admin";

        var user = await userManager.FindByEmailAsync(adminEmail);
        if (user == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminUsername,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = adminFirstName,
                LastName = adminLastName,
                IsModerator = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, adminRole);
            }
        }
        else
        {
            var roles = await userManager.GetRolesAsync(user);
            if (!roles.Contains("Admin"))
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }
            user.IsModerator = true;
            await userManager.UpdateAsync(user);
        }
    }
}