using Common.DotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
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
            Parameters.id = "summary";
            if (!Parameters.month.HasValue)
                Parameters.month = _clock.Now.Month;

            // TODO: Make this Async()
            Reports = new List<IEnumerable<IDisplayReport>>(_reports.BuildSummary(Parameters));
        }
    }
}
