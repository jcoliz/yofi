using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using YoFi.AspNet.Data;
using YoFi.Core.Reports;

namespace YoFi.AspNet.Pages
{
    [Authorize(Policy = "CanRead")]
    public class ReportsModel : PageModel
    {
        public static List<List<string>> Definitions = new List<List<string>>()
        {
            new List<string>() { "income","taxes" },
            new List<string>() { "expenses", "savings"}
        };

        public List<List<IDisplayReport>> Reports { get; private set; }

        [ViewData]
        public string report { get; set; }

        [ViewData]
        public int? month { get; set; }

        public ReportsModel(ApplicationDbContext context)
        {
            _builder = new ReportBuilder(context);
        }

        private readonly ReportBuilder _builder;

        public void OnGet([Bind("year,month")] ReportBuilder.Parameters parms)
        {
            // Fixup parameter
            if (!parms.month.HasValue)
                parms.month = DateTime.Now.Month;

            // Build the reports
            Reports = Definitions.Select(x => x.Select(y => _builder.BuildReport(new ReportBuilder.Parameters() { id = y, month = parms.month })).ToList<IDisplayReport>()).ToList();

            // Calculate the summary

            decimal TotalForReport(string name) => 
                Reports.SelectMany(x => x).Where(x => x.Name == name).First().GrandTotal;

            // Net Income: Income + Taxes

            var netincome = new ManualReport() { Name = "Net Income", Description = "--" };
            var incomerow = new RowLabel() { Name = "Income" };
            var taxesrow = new RowLabel() { Name = "Taxes" };
            var pctcol = new ColumnLabel() { Name = "% of Income", IsSortingAfterTotal = true, DisplayAsPercent = true };
            netincome[netincome.TotalColumn, incomerow] = TotalForReport("Income");
            netincome[pctcol, incomerow] = 1m;
            netincome[netincome.TotalColumn, taxesrow] = TotalForReport("Taxes");
            netincome[pctcol, taxesrow] = -TotalForReport("Taxes") / TotalForReport("Income");
            netincome[netincome.TotalColumn, netincome.TotalRow] = TotalForReport("Income") + TotalForReport("Taxes");
            netincome[pctcol, netincome.TotalRow] = netincome.GrandTotal / TotalForReport("Income");
            Reports[0].Add(netincome);

            // Profit: Income + Taxes + Expenses

            var profit = new ManualReport() { Name = "Profit" };
            var netincomerow = new RowLabel() { Name = "Net Income" };
            var expensesrow = new RowLabel() { Name = "Expenses" };
            pctcol = new ColumnLabel() { Name = "% of Net Income", IsSortingAfterTotal = true, DisplayAsPercent = true };
            profit[profit.TotalColumn, netincomerow] = netincome.GrandTotal;
            profit[pctcol, netincomerow] = 1m;
            profit[profit.TotalColumn, expensesrow] = TotalForReport("Expenses");
            profit[pctcol, expensesrow] = - TotalForReport("Expenses") / netincome.GrandTotal;
            profit[profit.TotalColumn, profit.TotalRow] = netincome.GrandTotal + TotalForReport("Expenses");
            profit[pctcol, profit.TotalRow] = (netincome.GrandTotal + TotalForReport("Expenses")) / netincome.GrandTotal;
            Reports[1].Add(profit);

            // Budget Reports: I think I want Budget vs Expenses on the left, and perhaps managed budget on the right

            report = "summary";
            month = parms.month;
        }
    }
}
