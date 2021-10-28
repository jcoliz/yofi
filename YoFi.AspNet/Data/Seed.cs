using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using YoFi.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Boilerplate.Models;
using YoFi.Core.SampleGen;
using Common.NET.Data;

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

        /// <summary>
        /// Add sample data to the database
        /// </summary>
        /// <remarks>
        /// ONLY IF: This is not a branded site, AND there is no data of any time already there
        /// </remarks>
        /// <returns></returns>
        public static void AddSampleData(ApplicationDbContext context, IConfiguration Configuration)
        {
            if (!Configuration.GetSection("Brand").Exists())
            {
                if (! context.Transactions.Any() && ! context.Payees.Any() && ! context.BudgetTxs.Any() )
                {
                    // Load sample data
                    var instream = SampleData.Open("FullSampleDataDefinition.xlsx");
                    var generator = new SampleDataGenerator();
                    generator.LoadDefinitions(instream);
                    generator.GenerateTransactions(addids: false);
                    generator.GeneratePayees();
                    generator.GenerateBudget();

                    // Insert into database
                    context.Transactions.AddRange(generator.Transactions);
                    context.Payees.AddRange(generator.Payees);
                    context.BudgetTxs.AddRange(generator.BudgetTxs);
                    context.SaveChanges();
                }
            }
        }
    }
}
