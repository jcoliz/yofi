using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using YoFi.Core.Reports;

namespace YoFi.AspNet.Pages
{
    [Authorize(Policy = "CanRead")]
    public class ReportsModel : PageModel, IReportNavbarViewModel
    {
        public List<IEnumerable<IDisplayReport>> Reports { get; private set; }

        public ReportParameters Parameters { get; private set; }

        IEnumerable<ReportDefinition> IReportNavbarViewModel.Definitions => _reports.Definitions;

        public ReportsModel(IReportEngine reports)
        {
            _reports = reports;
        }

        private readonly IReportEngine _reports;

        public void OnGet([Bind("year,month")] ReportParameters parms)
        {
            Parameters = parms;
            Parameters.id = "summary";
            if (!Parameters.month.HasValue)
                Parameters.month = DateTime.Now.Month;

            // TODO: Make this Async()
            Reports = new List<IEnumerable<IDisplayReport>>(_reports.BuildSummary(Parameters));
        }
    }
}
