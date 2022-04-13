using Microsoft.AspNetCore.Identity;
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

namespace YoFi.Data
{
    // https://github.com/temilaj/ASP.NET-core-role-based-authentication/blob/version/dotnet-core-2-0/Data/Seed.cs
    // https://gooroo.io/GoorooTHINK/Article/17333/Custom-user-roles-and-rolebased-authorization-in-ASPNET-core/28380#.WxwmNExFyAd
    public static class Seed
    {
        /// <summary>
        /// Add sample data to the database
        /// </summary>
        /// <remarks>
        /// ONLY IF: The demo is enabled, AND there is no data of any time already there
        /// </remarks>
        /// <returns></returns>
        public static async Task ManageSampleData(ISampleDataProvider loader, IDataAdminProvider dbadmin)
        {
            var status = await dbadmin.GetDatabaseStatus();
            if (status.IsEmpty)
                await loader.SeedAsync("all", hidden: true);

            await dbadmin.UnhideTransactionsToToday();
        }
    }
}
