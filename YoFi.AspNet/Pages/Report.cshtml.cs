using Common.DotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using YoFi.AspNet.Pages.Helpers;
using YoFi.Core.Reports;

namespace YoFi.AspNet.Pages
{
    /// <summary>
    /// Page to display a single report as described in the GET parameters
    /// </summary>
    /// <remarks>
    /// Note this is just a shell. The report generation is done in the background by ReportPartial.
    /// </remarks>
    [Authorize(Policy = "CanRead")]
    public class ReportModel : PageModel, IReportNavbarViewModel, IReportAndChartViewModel
    {
        public ReportModel(IReportEngine reports, IClock clock)
        {
            _reports = reports;
            _clock = clock;
        }

        public string Title { get; set; }

        public ReportParameters Parameters { get; set; }

        IEnumerable<ReportDefinition> IReportNavbarViewModel.Definitions => _reports.Definitions;

        public Report Report { get; set; }

        public string ChartJson { get; set; } = null;

        public bool ShowSideChart { get; set; } = false;

        public bool ShowTopChart { get; set; } = false;

        IDisplayReport IReportAndChartViewModel.Report => Report;

        // TODO: See AB#1186. We would like to return the possible number of levels we COULD run this report with.
        // However, at the time we NEED to know this, we DON'T know it yet. So for now I'm putting the interface
        // framing in, but I don't yet know how to handle this.
        int IReportNavbarViewModel.MaxLevels => Report?.MaxLevels ?? 4;

        public void OnGet([Bind] ReportParameters parms)
        {
            Parameters = parms;

            if (string.IsNullOrEmpty(parms.slug))
            {
                parms.slug = "all";
            }

            var sessionvars = new SessionVariables(HttpContext);

            if (parms.year.HasValue)
                sessionvars.Year = parms.year.Value;
            else
                parms.year = sessionvars.Year ?? _clock.Now.Year;

            if (!parms.month.HasValue)
            {
                bool iscurrentyear = (parms.year == _clock.Now.Year);

                // By default, month is the current month when looking at the current year.
                // When looking at previous years, default is the whole year (december)
                if (iscurrentyear)
                    parms.month = _clock.Now.Month;
                else
                    parms.month = 12;
            }

            Title = _reports.Definitions.Where(x=>x.slug == parms.slug).SingleOrDefault()?.Name ?? "Not Found";
        }

        private readonly IReportEngine _reports;
        private readonly IClock _clock;
    }
}
