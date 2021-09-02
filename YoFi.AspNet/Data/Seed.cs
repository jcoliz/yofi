using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using YoFi.AspNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Boilerplate.Models;

namespace YoFi.AspNet.Data
{
    // https://github.com/temilaj/ASP.NET-core-role-based-authentication/blob/version/dotnet-core-2-0/Data/Seed.cs
    // https://gooroo.io/GoorooTHINK/Article/17333/Custom-user-roles-and-rolebased-authorization-in-ASPNET-core/28380#.WxwmNExFyAd
    public static class Seed
    {
        public static async Task CreateRoles(IServiceProvider serviceProvider, IConfiguration Configuration)
        {
            //adding custom roles
            var RoleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var UserManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
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
            var adminusersection = Configuration.GetSection("AdminUser");

            if (null != adminusersection && adminusersection.Exists())
            {
                var poweruser = new ApplicationUser
                {
                    UserName = adminusersection["Email"],
                    Email = adminusersection["Email"]
                };

                string UserPassword = adminusersection["Password"];
                var _user = await UserManager.FindByEmailAsync(adminusersection["Email"]);

                if (_user == null)
                {
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
}
