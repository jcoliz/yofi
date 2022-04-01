using Common.DotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using YoFi.AspNet.Pages.Helpers;
using YoFi.Core.Reports;

namespace YoFi.AspNet.Pages
{
    /// <summary>
    /// Page which creates a summary of individual reports to provide an overall
    /// picture of the users' finances
    /// </summary>
    [Authorize(Policy = "CanRead")]
    public class ReportsModel : PageModel, IReportNavbarViewModel
    {
        public List<IEnumerable<IDisplayReport>> Reports { get; private set; }

        public ReportParameters Parameters { get; private set; }

        IEnumerable<ReportDefinition> IReportNavbarViewModel.Definitions => _reports.Definitions;

        int IReportNavbarViewModel.MaxLevels => 1;

        public ReportsModel(IReportEngine reports, IClock clock)
        {
            _reports = reports;
            _clock = clock;
        }

        private readonly IReportEngine _reports;
        private readonly IClock _clock;

        public void OnGet([Bind("year,month")] ReportParameters parms)
        {
            Parameters = parms;
            Parameters.slug = "summary";

            var sessionvars = new SessionVariables(HttpContext);

            if (Parameters.year.HasValue)
                sessionvars.Year = Parameters.year.Value;
            else
                Parameters.year = sessionvars.Year ?? _clock.Now.Year;

            if (!Parameters.month.HasValue)
            {
                bool iscurrentyear = (Parameters.year == _clock.Now.Year);

                // By default, month is the current month when looking at the current year.
                // When looking at previous years, default is the whole year (december)
                if (iscurrentyear)
                    Parameters.month = _clock.Now.Month;
                else
                    Parameters.month = 12;
            }

            // TODO: Make this Async()
            Reports = new List<IEnumerable<IDisplayReport>>(_reports.BuildSummary(Parameters));
        }
    }
}
