using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Common.ChartJS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YoFi.Core.Reports;

namespace YoFi.AspNet.Pages
{
    public class BudgetModel : PageModel
    {
        private readonly IReportEngine _reportengine;

        public Report Report { get; set; }

        public string ChartJson { get; set; }

        public BudgetModel(IReportEngine reportengine)
        {
            _reportengine = reportengine;
        }

        public void OnGet()
        {
            Report = _reportengine.Build(new ReportParameters() { id = "expenses-v-budget" });

            // Flip the sign on values unless they're ordred ascending
            decimal factor = Report.SortOrder == Report.SortOrders.TotalAscending ? 1m : -1m;

            var rows = Report.RowLabelsOrdered.Where(x => !x.IsTotal && x.Parent == null);
            var labels = rows.Select(x => x.Name);

            var cols = Report.ColumnLabelsFiltered.Where(x => !x.IsTotal && !x.IsCalculated);
            var series = cols.Select(col => (col.Name, rows.Select(row => (int)(Report[col, row] * factor))));
            var Chart = ChartConfig.CreateMultiBarChart(labels, series, palette);

            // TODO: Need to scale down the budget based on the %complete of the year

            ChartJson = JsonSerializer.Serialize(Chart, new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }); ;
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
