using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using YoFi.Data;

namespace YoFi.Main.Seeders;

// https://github.com/temilaj/ASP.NET-core-role-based-authentication/blob/version/dotnet-core-2-0/Data/Seed.cs
// https://gooroo.io/GoorooTHINK/Article/17333/Custom-user-roles-and-rolebased-authorization-in-ASPNET-core/28380#.WxwmNExFyAd
public static class IdentitySeeder
{
    public static async Task SeedIdentity(IServiceProvider serviceProvider)
    {
        //adding custom roles
        var RoleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var UserManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminUserConfig = serviceProvider.GetRequiredService<IOptions<AdminUserConfig>>();

        string[] roleNames = { "Admin", "Verified", "Unknown" };
        IdentityResult roleResult;

        foreach (var roleName in roleNames)
        {
            //creating the roles and seeding them to the database
            var roleExist = await RoleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                roleResult = await RoleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        //creating a super user who could maintain the web app
        if (adminUserConfig?.Value?.Email != null)
        {
            var poweruser = new ApplicationUser
            {
                UserName = adminUserConfig.Value.Email,
                Email =  adminUserConfig.Value.Email
            };

            var _user = await UserManager.FindByEmailAsync( adminUserConfig.Value.Email);

            if (_user == null)
            {
                string UserPassword = adminUserConfig.Value.Password;
                var createPowerUser = await UserManager.CreateAsync(poweruser, UserPassword);
                if (createPowerUser.Succeeded)
                {
                    //here we tie the new user to the "Admin" role 
                    await UserManager.AddToRoleAsync(poweruser, "Admin");
                    await UserManager.AddToRoleAsync(poweruser, "Verified");
                }
            }
        }
    }
}