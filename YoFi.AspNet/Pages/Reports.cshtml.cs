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
        public static List<ReportDefinition> Definitions = new List<ReportDefinition>()
        {
            new ReportDefinition()
            {
                id = "income",
                SkipLevels = 1,
                //DisplayLevelAdjustment = 1,
                SortOrder = "TotalAscending",
                Name = "Income",
                Source = "Actual",
                SourceParameters = "top=Income",
                CustomColumns = "pctoftotal"
            },
            new ReportDefinition()
            {
                id = "taxes",
                Source = "Actual",
                SourceParameters = "top=Taxes",
                SkipLevels = 1,
                //DisplayLevelAdjustment = 1,
                Name = "Taxes",
                CustomColumns = "pctoftotal"
            },
            new ReportDefinition()
            {
                id = "expenses",
                CustomColumns = "pctoftotal",
                Source = "Actual",
                SourceParameters = "excluded=Savings,Taxes,Income,Transfer,Unmapped",
                //DisplayLevelAdjustment = 1,
                Name = "Expenses"
            },
            new ReportDefinition()
            {
                id = "savings",
                Source = "Actual",
                SourceParameters = "top=Savings",
                SkipLevels = 1,
                //DisplayLevelAdjustment = 1,
                Name = "Savings",
                CustomColumns = "pctoftotal"
            },
        };

        public List<Report> Reports { get; private set; }

        public List<Table<ColumnLabel, RowLabel, decimal>> Tables { get; private set; }


        public ReportsModel(ApplicationDbContext context)
        {
            _builder = new ReportBuilder(context);
        }

        private readonly ReportBuilder _builder;

        public void OnGet()
        {
            // Build the reports

            Reports = Definitions.Select(x => _builder.BuildReport(new ReportBuilder.Parameters(),x)).ToList();

            // Calculate the summary

            Tables = new List<Table<ColumnLabel, RowLabel, decimal>>();

            var ReportsByName = Reports.ToDictionary(x => x.Name, x => x);

            // Net Income: Income + Taxes

            var netincome = new Table<ColumnLabel, RowLabel, decimal>();
            var totalcolumn = new ColumnLabel() { IsTotal = true };
            var totalrow = new RowLabel() { IsTotal = true };
            var incomerow = new RowLabel() { Name = "Income" };
            var taxesrow = new RowLabel() { Name = "Taxes" };
            netincome[totalcolumn, incomerow] = ReportsByName["Income"].GrandTotal;
            netincome[totalcolumn, taxesrow] = ReportsByName["Taxes"].GrandTotal;
            var netincometotal = netincome[totalcolumn, totalrow] = ReportsByName["Income"].GrandTotal + ReportsByName["Taxes"].GrandTotal;
            Tables.Add(netincome);

            // Profit: Income + Taxes + Expenses

            var profit = new Table<ColumnLabel, RowLabel, decimal>();
            var netincomerow = new RowLabel() { Name = "Net Income" };
            var expensesrow = new RowLabel() { Name = "Expenses" };
            var pctcol = new ColumnLabel() { Name = "% of Net Income", IsSortingAfterTotal = true, DisplayAsPercent = true };
            //var savingsrow = new RowLabel() { Name = "Savings" };
            profit[totalcolumn, netincomerow] = netincometotal;
            profit[pctcol, netincomerow] = 1m;
            profit[totalcolumn, expensesrow] = ReportsByName["Expenses"].GrandTotal;
            profit[pctcol, expensesrow] = ReportsByName["Expenses"].GrandTotal / netincometotal;
            //profit[totalcolumn, savingsrow] = ReportsByName["Savings"].GrandTotal;
            profit[totalcolumn, totalrow] = netincometotal + ReportsByName["Expenses"].GrandTotal; //    + ReportsByName["Savings"].GrandTotal;
            profit[pctcol, totalrow] = (netincometotal + ReportsByName["Expenses"].GrandTotal) / netincometotal;
            Tables.Add(profit);

            // Savings Rate: ( Savings + Profit ) / ( Income + Taxes )

            // For visual consistency, I could make tables and render them like reports
        }
    }
}
