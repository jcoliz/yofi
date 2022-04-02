using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.DotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using YoFi.Core;
using YoFi.Core.SampleData;

namespace YoFi.AspNet.Pages
{
    [Authorize(Roles = "Admin")]
    public class AdminModel : PageModel
    {
        public PageConfig Config { get; private set; }
        public IEnumerable<ISampleDataSeedOffering> Offerings { get; private set; }
        public IDatabaseStatus DatabaseStatus { get; private set; }

        private readonly IDatabaseAdministration _dbadmin;
        private readonly ISampleDataLoader _loader;

        public class PageConfig
        {
            public const string Section = "Admin";

            public bool NoDelete { get; set; }
        }

        public AdminModel(IDatabaseAdministration dbadmin, IOptions<PageConfig> config, ISampleDataLoader loader)
        {
            _dbadmin = dbadmin;
            Config = config.Value;
            _loader = loader;
        }

        public async Task OnGetAsync()
        {
            var getofferings = await _loader.ListSeedOfferingsAsync();
            Offerings = getofferings.Where(x => x.IsAvailable).ToList();

            DatabaseStatus = await _dbadmin.GetDatabaseStatus();
        }
    }
}
