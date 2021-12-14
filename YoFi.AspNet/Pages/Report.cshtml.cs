using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YoFi.AspNet.Pages.Charting;
using YoFi.Core;
using YoFi.Core.Reports;

namespace YoFi.AspNet.Pages
{
    [Authorize(Policy = "CanRead")]
    public class ReportModel : PageModel, IReportNavbarViewModel
    {
        public ReportModel(IReportEngine _reports)
        {
            reports = _reports;
        }

        public ReportParameters Parameters { get; set; }

        IEnumerable<ReportDefinition> IReportNavbarViewModel.Definitions => reports.Definitions;

        public Report Report { get; set; }

        public string ChartJson { get; set; }

        public Task<IActionResult> OnGetAsync([Bind] ReportParameters parms)
        {
            try
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

                // TODO: Make this Async()
                Report = reports.Build(Parameters);

                /*
                ViewData["report"] = parms.id;
                ViewData["month"] = parms.month;
                ViewData["level"] = result.NumLevels;
                ViewData["showmonths"] = result.WithMonthColumns;
                ViewData["Title"] = result.Name;
                */

                // Make a chart

                var palette = new ChartColor[]
                {
                    new ChartColor("540D6E"),
                    new ChartColor("EE4266"),
                    new ChartColor("FFD23F"),
                    new ChartColor("313628"),
                    new ChartColor("3A6EA5"),
                    new ChartColor("7F7C4A"),
                    new ChartColor("7A918D"),
                };

                var Chart = new Charting.ChartDef() { Type = "doughnut" };
                Chart.Data.Labels = Report.RowLabelsOrdered.Where(x=>!x.IsTotal).Select(x=>x.Name).ToArray();
                Chart.Data.Datasets[0].Data = Report.RowLabelsOrdered.Where(x=>!x.IsTotal).Select(x=>(int)(Math.Abs(Report[Report.TotalColumn,x]))).ToArray();
                var numitems = Report.RowLabelsOrdered.Where(x=>!x.IsTotal).Count();
                Chart.Data.Datasets[0].BackgroundColor = palette.Take(numitems).Select(x => x.WithAlpha(0.5)).ToArray();
                Chart.Data.Datasets[0].BorderColor = palette.Take(numitems).ToArray();

                ChartJson = System.Text.Json.JsonSerializer.Serialize(Chart, new System.Text.Json.JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }); ;

                // TODO: Limit to 7 items max. Put the rest under "others"
                // TODO: Pick only top rows

                return Task.FromResult(Page() as IActionResult);
            }
            catch (KeyNotFoundException ex)
            {
                return Task.FromResult(NotFound(ex.Message) as IActionResult);
            }
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
        private readonly IReportEngine reports;
    }
}
