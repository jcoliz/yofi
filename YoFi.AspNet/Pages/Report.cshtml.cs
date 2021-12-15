using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

        public string ChartJson { get; set; } = null;

        public bool ShowSideChart { get; set; } = false;

        public bool ShowTopChart { get; set; } = false;

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

                // Should we Make a chart??

                // We show a pie chart to the side of the report when all of the following are true:
                //  - There are no months being shown
                //  - There is only one sign of information shown (all negative or all positive). However this isn't always the case :| If there is an item that's small
                // off-sign it may be OK.
                //  - There is a total column
                var multisigned = Report.Source.Any(x => x.IsMultiSigned);
                if (! Report.WithMonthColumns && Report.WithTotalColumn && ! multisigned)
                {
                    // We have to put a little more thought into whether this is a single-sign or multiple-sign report.
                    // Practically speaking, it's only an issue when we're showing "Income" and at least one other top-level row.
                    // It SEEMS we can do this based on definition.SoureParameters. If it's empty, it is an "all" type report which
                    // will have income AND expenses, so that's not cool for a pie report

                    ShowSideChart = true;

                    var labels = Report.RowLabelsOrdered.Where(x => !x.IsTotal && x.Parent == null);
                    var points = labels.Select(x => new ChartDataPoint() { Label = x.Name, Data = (int)(Math.Abs(Report[Report.TotalColumn, x])) });
                    var Chart = new ChartDef() { Type = "doughnut" };
                    Chart.SetDataPoints(points);

                    ChartJson = JsonSerializer.Serialize(Chart, new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }); ;
                }

                if (Report.WithMonthColumns)
                {
                    ShowTopChart = true;

                    // Flip the sign on values unless they're ordred ascending
                    decimal factor = Report.SortOrder == Report.SortOrders.TotalAscending ? 1m : -1m;
                    var cols = Report.ColumnLabelsFiltered.Where(x => !x.IsTotal && !x.IsCalculated);
                    var labels = cols.Select(x => x.Name);
                    var rows = Report.RowLabelsOrdered.Where(x => !x.IsTotal && x.Parent == null);
                    var series = rows.Select(row => new ChartDataSeries() { Label = row.Name, Data = cols.Select(col => (int)(Report[col, row]*factor)) });
                    var Chart = new ChartDef() { Type = "line" };
                    Chart.SetDataSeries(labels,series);

                    ChartJson = JsonSerializer.Serialize(Chart, new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }); ;
                }

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
