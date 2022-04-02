﻿using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using YoFi.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Boilerplate.Models;
using YoFi.Core.SampleData;
using Common.DotNet.Data;
using Common.DotNet;
using YoFi.Core;

namespace YoFi.AspNet.Data
{
    // https://github.com/temilaj/ASP.NET-core-role-based-authentication/blob/version/dotnet-core-2-0/Data/Seed.cs
    // https://gooroo.io/GoorooTHINK/Article/17333/Custom-user-roles-and-rolebased-authorization-in-ASPNET-core/28380#.WxwmNExFyAd
    public static class Seed
    {
        public static async Task CreateRoles(IServiceProvider serviceProvider, IOptions<AdminUserConfig> adminUserConfig)
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

        /// <summary>
        /// Add sample data to the database
        /// </summary>
        /// <remarks>
        /// ONLY IF: The demo is enabled, AND there is no data of any time already there
        /// </remarks>
        /// <returns></returns>
        public static async Task ManageSampleData(ISampleDataLoader loader, IDatabaseAdministration dbadmin)
        {
            var status = await dbadmin.GetDatabaseStatus();
            if (status.IsEmpty)
                await loader.SeedAsync("all", hidden: true);

            await dbadmin.UnhideTransactionsToToday();
        }
    }
}
