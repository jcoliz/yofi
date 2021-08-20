using System;
using System.Collections.Generic;
using System.Linq;
using YoFi.AspNet.Data;

namespace YoFi.AspNet.Controllers.Reports
{
    /// <summary>
    /// Build application-specific reports
    /// </summary>
    /// <remarks>
    /// This is where the app logic lives to arrange the data we have in our
    /// database into reports that will be interesting for the user.
    /// 
    /// It would be possible to make this more data driven so that fully
    /// custom reports could be created.
    /// </remarks>
    public class ReportBuilder
    {
        /// <summary>
        /// Parameters used to build a report
        /// </summary>
        /// <remarks>
        /// Moved these into a class so I can make a single change in calling convention here
        /// and have it propagate out to the controller endpoints automatically
        /// </remarks>
        public class Parameters
        {
            public string id { get; set; } 
            public int? year { get; set; } 
            public int? month { get; set; } 
            public bool? showmonths { get; set; } 
            public int? level { get; set; }
        }

        public ReportBuilder(ApplicationDbContext context)
        {
            _context = context;
            _qbuilder = new QueryBuilder(context);
        }

        private int Year;
        private int Month;
        private readonly ApplicationDbContext _context;
        private readonly QueryBuilder _qbuilder;

        public Report BuildReport(Parameters parms)
        {
            var result = new Report();

            _qbuilder.Month = Month = parms.month ?? 12;
            _qbuilder.Year = Year = parms.year ?? DateTime.Now.Year;

            var period = new DateTime(Year, Month, 1);
            result.Description = $"For {Year} through {period.ToString("MMMM")} ";
            
            var notexpenses = new List<string>() { "Savings", "Taxes", "Income", "Transfer", "Unmapped" };
            
            var budgetpctcolumn = new ColumnLabel()
            {
                Name = "% Progress",
                UniqueID = "Z",
                DisplayAsPercent = true,
                Custom = (cols) =>
                    cols.GetValueOrDefault("ID:Budget") == 0 || (Math.Abs(cols.GetValueOrDefault("ID:Actual") / cols.GetValueOrDefault("ID:Budget")) > 10m ) ? 
                        0 : 
                        cols.GetValueOrDefault("ID:Actual") / cols.GetValueOrDefault("ID:Budget")
            };

            var pctoftotalcolumn = new ColumnLabel()
            {
                Name = "% Total",
                IsSortingAfterTotal = true,
                DisplayAsPercent = true,
                Custom = (cols) =>
                    result.GrandTotal == 0 ? 0 : cols.GetValueOrDefault("TOTAL") / result.GrandTotal
            };

            var budgetavailablecolumn = new ColumnLabel()
            {
                Name = "Available",
                Custom = (cols) =>
                    cols.GetValueOrDefault("ID:Actual") - cols.GetValueOrDefault("ID:Budget")
            };

            if (parms.id == "all")
            {
                result.WithMonthColumns = true;
                result.NumLevels = 2;
                result.Source = _qbuilder.QueryTransactionsComplete();
                result.SortOrder = Report.SortOrders.TotalDescending;
                result.Name = "All Transactions";
            }
            else if (parms.id == "income")
            {
                result.AddCustomColumn(pctoftotalcolumn);
                result.Source = _qbuilder.QueryTransactionsComplete(top: "Income");
                result.SkipLevels = 1;
                result.DisplayLevelAdjustment = 1;
                result.SortOrder = Report.SortOrders.TotalAscending;
                result.Name = "Income";
            }
            else if (parms.id == "taxes")
            {
                result.AddCustomColumn(pctoftotalcolumn);
                result.Source = _qbuilder.QueryTransactionsComplete(top: "Taxes");
                result.SkipLevels = 1;
                result.DisplayLevelAdjustment = 1;
                result.SortOrder = Report.SortOrders.TotalDescending;
                result.Name = "Taxes";
            }
            else if (parms.id == "savings")
            {
                result.AddCustomColumn(pctoftotalcolumn);
                result.Source = _qbuilder.QueryTransactionsComplete(top: "Savings");
                result.SkipLevels = 1;
                result.DisplayLevelAdjustment = 1;
                result.SortOrder = Report.SortOrders.TotalDescending;
                result.Name = "Savings";
            }
            else if (parms.id == "expenses")
            {
                result.AddCustomColumn(pctoftotalcolumn);
                result.WithMonthColumns = true;
                result.Source = _qbuilder.QueryTransactionsCompleteExcept(tops: notexpenses);
                result.NumLevels = 2;
                result.SortOrder = Report.SortOrders.TotalDescending;
                result.Name = "Expenses";
            }
            else if (parms.id == "expenses-budget")
            {
                _qbuilder.Month = 12; // Budget reports are whole-year, generally
                result.Description = $"For {Year}";
                result.Source = _qbuilder.QueryBudgetExcept(tops: notexpenses);
                result.NumLevels = 3;
                result.SortOrder = Report.SortOrders.TotalDescending;
                result.Name = "Expenses Budget";
            }
            else if (parms.id == "expenses-v-budget")
            {
                _qbuilder.Month = 12; // Budget reports are whole-year, generally
                var source = _qbuilder.QueryActualVsBudgetExcept(tops: notexpenses);
                result.Source = source;

                // What is the highest transaction in the "Actuals"?
                var latesttime = source.Where(x => x.Name == "Actual").Select(q => q.Query.DefaultIfEmpty().Max(a => (a==null)?DateTime.MinValue:a.Timestamp )).Max();

                // What % of the way is it through that year?
                var yearprogress = (double)latesttime.DayOfYear / 365.0;

                result.Description = $"For {Year} ({yearprogress:P0})";
                result.AddCustomColumn(budgetpctcolumn);
                result.WithTotalColumn = false;
                result.NumLevels = 3;
                result.SortOrder = Report.SortOrders.TotalDescending;
                result.Name = "Expenses vs. Budget";
            }
            else if (parms.id == "all-v-budget")
            {
                _qbuilder.Month = 12; // Budget reports are whole-year, generally
                result.Description = $"For {Year}";
                result.AddCustomColumn(budgetpctcolumn);
                result.Source = _qbuilder.QueryActualVsBudget();
                result.WithTotalColumn = false;
                result.NumLevels = 3;
                result.SortOrder = Report.SortOrders.TotalDescending;
                result.Name = "All vs. Budget";
            }
            else if (parms.id == "managed-budget")
            {
                result.AddCustomColumn(budgetpctcolumn);
                result.AddCustomColumn(budgetavailablecolumn);
                result.Source = _qbuilder.QueryManagedBudget();
                result.WithTotalColumn = false;
                result.NumLevels = 4;
                result.DisplayLevelAdjustment = 1;
                result.SortOrder = Report.SortOrders.NameAscending;
                result.Name = "Managed Budget";
            }
            else if (parms.id == "budget")
            {
                _qbuilder.Month = 12; // Budget reports are whole-year, generally
                result.Description = $"For {Year}";
                result.NumLevels = 3;
                result.Source = _qbuilder.QueryBudget();
                result.SortOrder = Report.SortOrders.TotalDescending;
                result.Name = "Full Budget";
            }
            else if (parms.id == "yoy")
            {
                var years = new int[] { };
                result.Source = _qbuilder.QueryYearOverYear(out years);
                result.Description = $"For {years.Min()} to {years.Max()}";
                result.NumLevels = 3;
                result.SortOrder = Report.SortOrders.TotalDescending;
                result.Name = "Year over Year";
            }
            else if (parms.id == "export")
            {
                result.Source = _qbuilder.QueryActualVsBudget(leafrows:true);
                result.WithTotalColumn = false;
                result.NumLevels = 4;
                result.SortOrder = Report.SortOrders.NameAscending;
                result.Name = "Transaction Export";
            }

            if (parms.level.HasValue)
            {
                result.NumLevels = parms.level.Value;
                if (result.NumLevels == 1)
                    result.DisplayLevelAdjustment = 1;
            }

            if (parms.showmonths.HasValue)
                result.WithMonthColumns = parms.showmonths.Value;

            result.Build();
            result.WriteToConsole();

            return result;
        }
   }
}
