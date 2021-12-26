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
    [Authorize(Policy = "CanRead")]
    public class ReportModel : PageModel, IReportNavbarViewModel, IReportAndChartViewModel
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

        IDisplayReport IReportAndChartViewModel.Report => Report;

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
                var multisigned = Report.Source?.Any(x => x.IsMultiSigned) ?? true;
                if (! Report.WithMonthColumns && ! multisigned)
                {
                    // Flip the sign on values unless they're ordred ascending
                    decimal factor = Report.SortOrder == Report.SortOrders.TotalAscending ? 1m : -1m;

                    if (Report.WithTotalColumn)
                    {
                        // We have to put a little more thought into whether this is a single-sign or multiple-sign report.
                        // Practically speaking, it's only an issue when we're showing "Income" and at least one other top-level row.
                        // It SEEMS we can do this based on definition.SoureParameters. If it's empty, it is an "all" type report which
                        // will have income AND expenses, so that's not cool for a pie report

                        ShowSideChart = true;

                        var col = Report.TotalColumn;
                        var rows = Report.RowLabelsOrdered.Where(x => !x.IsTotal && x.Parent == null);
                        var points = rows.Select(row => (row.Name, (int)(Report[col, row] * factor)));

                        ChartConfig Chart = null;

                        // Pie chart is only for all-positive values, else bar chart
                        if (points.All(x => x.Item2 >= 0))
                            Chart = ChartConfig.CreatePieChart(points, palette);
                        else
                            Chart = ChartConfig.CreateBarChart(points, palette);

                        ChartJson = JsonSerializer.Serialize(Chart, new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }); ;
                    }
                    else
                    {
                        // If it has no total column, we will plot a multi-bar chart on the top using each series column as a series
                        ShowTopChart = true;

                        var rows = Report.RowLabelsOrdered.Where(x => !x.IsTotal && x.Parent == null);
                        var labels = rows.Select(x => x.Name);

                        var cols = Report.ColumnLabelsFiltered.Where(x => !x.IsTotal && !x.IsCalculated);
                        var series = cols.Select(col => (col.Name, rows.Select(row => (int)(Report[col, row] * factor))));
                        var Chart = ChartConfig.CreateMultiBarChart(labels, series, palette);

                        // TODO: Need to scale down the budget based on the %complete of the year

                        ChartJson = JsonSerializer.Serialize(Chart, new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }); ;
                    }
                }

                if (Report.WithMonthColumns)
                {
                    ShowTopChart = true;

                    // Flip the sign on values unless they're ordred ascending
                    decimal factor = Report.SortOrder == Report.SortOrders.TotalAscending ? 1m : -1m;

                    var cols = Report.ColumnLabelsFiltered.Where(x => !x.IsTotal && !x.IsCalculated);
                    var labels = cols.Select(x => x.Name);
                    var rows = Report.RowLabelsOrdered.Where(x => !x.IsTotal && x.Parent == null);
                    var series = rows.Select(row => (row.Name, cols.Select(col => (int)(Report[col, row]*factor))));                    
                    var Chart = ChartConfig.CreateLineChart(labels, series, palette);

                    ChartJson = JsonSerializer.Serialize(Chart, new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }); ;
                }

                return Task.FromResult(Page() as IActionResult);
            }
            catch (KeyNotFoundException ex)
            {
                return Task.FromResult(NotFound(ex.Message) as IActionResult);
            }
        }

        private static readonly ChartColor[] palette = new ChartColor[]
        {
            new ChartColor("540D6E"),
            new ChartColor("EE4266"),
            new ChartColor("FFD23F"),
            new ChartColor("875D5A"),
            new ChartColor("FFD3DA"),
            new ChartColor("8EE3EF"),
            new ChartColor("7A918D"),
        };

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
