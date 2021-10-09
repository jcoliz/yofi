using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YoFi.AspNet.Controllers.Reports;
using YoFi.AspNet.Data;

namespace YoFi.AspNet.Pages
{
    public class ReportsModel : PageModel
    {
        public static List<List<string>> Definitions = new List<List<string>>()
        {
            new List<string>() { "income","taxes" },
            new List<string>() { "expenses", "savings"}
        };

        public List<List<IDisplayReport>> Reports { get; private set; }

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

            var ReportsByName = Reports.SelectMany(x=>x).ToDictionary(x => x.Name, x => x);

            // Net Income: Income + Taxes

            var netincome = new ManualReport() { Name = "Net Income", Description = "--" };
            var incomerow = new RowLabel() { Name = "Income" };
            var taxesrow = new RowLabel() { Name = "Taxes" };
            var pctcol = new ColumnLabel() { Name = "% of Income", IsSortingAfterTotal = true, DisplayAsPercent = true };
            netincome[netincome.TotalColumn, incomerow] = ReportsByName["Income"].GrandTotal;
            netincome[pctcol, incomerow] = 1m;
            netincome[netincome.TotalColumn, taxesrow] = ReportsByName["Taxes"].GrandTotal;
            netincome[pctcol, taxesrow] = - ReportsByName["Taxes"].GrandTotal / ReportsByName["Income"].GrandTotal;
            netincome[netincome.TotalColumn, netincome.TotalRow] = ReportsByName["Income"].GrandTotal + ReportsByName["Taxes"].GrandTotal;
            netincome[pctcol, netincome.TotalRow] = netincome.GrandTotal / ReportsByName["Income"].GrandTotal;
            Reports[0].Add(netincome);

            // Profit: Income + Taxes + Expenses

            var profit = new ManualReport() { Name = "Profit" };
            var netincomerow = new RowLabel() { Name = "Net Income" };
            var expensesrow = new RowLabel() { Name = "Expenses" };
            pctcol = new ColumnLabel() { Name = "% of Net Income", IsSortingAfterTotal = true, DisplayAsPercent = true };
            //var savingsrow = new RowLabel() { Name = "Savings" };
            profit[profit.TotalColumn, netincomerow] = netincome.GrandTotal;
            profit[pctcol, netincomerow] = 1m;
            profit[profit.TotalColumn, expensesrow] = ReportsByName["Expenses"].GrandTotal;
            profit[pctcol, expensesrow] = - ReportsByName["Expenses"].GrandTotal / netincome.GrandTotal;
            //profit[totalcolumn, savingsrow] = ReportsByName["Savings"].GrandTotal;
            profit[profit.TotalColumn, profit.TotalRow] = netincome.GrandTotal + ReportsByName["Expenses"].GrandTotal; //    + ReportsByName["Savings"].GrandTotal;
            profit[pctcol, profit.TotalRow] = (netincome.GrandTotal + ReportsByName["Expenses"].GrandTotal) / netincome.GrandTotal;
            Reports[1].Add(profit);

            // Savings Rate: ( Savings + Profit ) / ( Income + Taxes )

            // For visual consistency, I could make tables and render them like reports
        }
    }
}
