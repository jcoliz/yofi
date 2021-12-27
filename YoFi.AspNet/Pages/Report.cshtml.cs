using Common.ChartJS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YoFi.Core;
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
        public ReportModel(IReportEngine reports)
        {
            _reports = reports;
        }

        public string Title { get; set; }

        public ReportParameters Parameters { get; set; }

        IEnumerable<ReportDefinition> IReportNavbarViewModel.Definitions => _reports.Definitions;

        public Report Report { get; set; }

        public string ChartJson { get; set; } = null;

        public bool ShowSideChart { get; set; } = false;

        public bool ShowTopChart { get; set; } = false;

        IDisplayReport IReportAndChartViewModel.Report => Report;

        public void OnGet([Bind] ReportParameters parms)
        {
            Parameters = parms;

            if (string.IsNullOrEmpty(parms.id))
            {
                parms.id = "all";
            }

            if (parms.year.HasValue)
                Year = parms.year.Value;
            else
                parms.year = Year;

            if (!parms.month.HasValue)
            {
                bool iscurrentyear = (Year == Now.Year);

                // By default, month is the current month when looking at the current year.
                // When looking at previous years, default is the whole year (december)
                if (iscurrentyear)
                    parms.month = Now.Month;
                else
                    parms.month = 12;
            }

            Title = _reports.Definitions.Where(x=>x.id == parms.id).SingleOrDefault()?.Name ?? "Not Found";
        }

        /// <summary>
        /// Current default year
        /// </summary>
        /// <remarks>
        /// If you set this in the reports, it applies throughout the app,
        /// defaulting to that year.
        /// </remarks>
        private int Year
        {
            get
            {
                if (!_Year.HasValue)
                {
                    var value = HttpContext?.Session.GetString(nameof(Year));
                    if (string.IsNullOrEmpty(value))
                    {
                        Year = Now.Year;
                    }
                    else
                    {
                        _Year = (int.TryParse(value, out int y)) ? y : Now.Year;
                    }
                }

                return _Year.Value;
            }
            set
            {
                _Year = value;

                var serialisedDate = _Year.ToString();
                HttpContext?.Session.SetString(nameof(Year), serialisedDate);
            }
        }
        private int? _Year = null;

        /// <summary>
        /// Current datetime
        /// </summary>
        /// <remarks>
        /// Which may be overridden by tests
        /// </remarks>
        public DateTime Now
        {
            get
            {
                return _Now ?? DateTime.Now;
            }
            set
            {
                _Now = value;
            }
        }

        private DateTime? _Now;
        private readonly IReportEngine _reports;
    }
}
