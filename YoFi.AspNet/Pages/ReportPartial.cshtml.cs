using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Common.ChartJS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YoFi.Core.Reports;

namespace YoFi.AspNet.Pages
{
    /// <summary>
    /// Page which will return a partial view of the supplied report
    /// </summary>
    /// <remarks>
    /// This page does the actual work of generating the chart and budget.
    /// The idea is that the outer frame with a loading spinner was delivered to the
    /// user in response to their URL request, then in the background via AJAX, we are
    /// asked to do the work.
    /// </remarks>
    [Authorize(Policy = "CanRead")]
    public class ReportPartialModel : PageModel, IReportAndChartViewModel
    {
        private readonly IReportEngine _reportengine;

        public Report Report { get; set; }

        public string ChartJson { get; set; }

        public bool ShowSideChart { get; set; }

        public bool ShowTopChart { get; set; }

        IDisplayReport IReportAndChartViewModel.Report => Report;

        public ReportPartialModel(IReportEngine reportengine)
        {
            _reportengine = reportengine;
        }

        public Task<IActionResult> OnGetAsync([Bind] ReportParameters parms)
        {
            try
            {
                // NOTE that ReportPartial doesn't do anything to the parms.
                // Caller is expected to handle filing in defaults for parms.

                // TODO: Make this Async()
                Report = _reportengine.Build(parms);

                // We show a pie chart to the side of the report when all of the following are true:
                //  - There are no months being shown
                //  - There is only one sign of information shown (all negative or all positive). However this isn't always the case :| If there is an item that's small
                // off-sign it may be OK.
                //  - There is a total column
                var multisigned = Report.Source?.Any(x => x.IsMultiSigned) ?? true;
                if (!Report.WithMonthColumns && !multisigned)
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
                        var series = cols.Select(col => (col.Name, rows.Select(row => (int)(Report[col, row] * factor)))).ToList();

                        // Task 1232: Budget summary: Scale budget based on % completion of the year
                        //
                        // We need to scale ONLY the "Budget" column, and ONLY when the report has a non-zero "YearProgress" value

                        if (Report.YearProgress != 0.0)
                        {
                            var i = series.FindIndex(x => x.Name == "Budget");

                            // Rebuild the series with a factor
                            series[i] = (series[i].Name + " (YTD)", series[i].Item2.Select(x => (int)((double)x * Report.YearProgress)));
                        }

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
                    var series = rows.Select(row => (row.Name, cols.Select(col => (int)(Report[col, row] * factor))));
                    var Chart = ChartConfig.CreateLineChart(labels, series, palette);

                    ChartJson = JsonSerializer.Serialize(Chart, new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }); ;
                }

                //var result = Partial("DisplayReportAndChart", this as IReportAndChartViewModel);

                var result = new PartialViewResult
                {
                    ViewName = "DisplayReportAndChart",
                    ViewData = ViewData,
                };
                return Task.FromResult(result as IActionResult);
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
    }
}
