using Microsoft.EntityFrameworkCore;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Controllers.Reports
{
    public class Query : List<KeyValuePair<string, IQueryable<IReportable>>>
    {
        public Query() { }
        public Query(IQueryable<IReportable> single)
        {
            Add(new KeyValuePair<string, IQueryable<IReportable>>(string.Empty, single));
        }
        public Query(IEnumerable<KeyValuePair<string, IQueryable<IReportable>>> items)
        {
            AddRange(items);
        }

        public Query(params Query[] many)
        {
            foreach(var q in many)
                AddRange(q);
        }

        public Query Labeled(string label)
        {
            return new Query(this.Select(x => new KeyValuePair<string, IQueryable<IReportable>>(label, x.Value)));
        }

        public void Add(string key, IQueryable<IReportable> value)
        {
            Add(new KeyValuePair<string, IQueryable<IReportable>>(key, value));
        }

    }
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
        private readonly ApplicationDbContext _context;

        public ReportBuilder(ApplicationDbContext context)
        {
            _context = context;
        }

        private int Year;
        private int Month;

        // NOTE: These ToList()'s and AsEnumerable()'s have been added wherever Entity Framework couldn't build queries
        // in the report generator. AsEnumerable() is preferred, as it defers execution. However in some cases, THAT 
        // doesn't even work, so I needed ToList().

        public Query QueryTransactions(string top = null)
        {
            var txs = _context.Transactions
                .Include(x => x.Splits)
                .Where(x => x.Timestamp.Year == Year && x.Hidden != true && x.Timestamp.Month <= Month)
                .Where(x => !x.Splits.Any());

            if (top != null)
            {
                string ecolon = $"{top}:";
                txs = txs.Where(x => x.Category == top || x.Category.StartsWith(ecolon));
            }

            return new Query(txs);
        }

        public Query QueryTransactionsExcept(IEnumerable<string> excluetopcategories)
        {
            var excluetopcategoriesstartswith = excluetopcategories
                .Select(x => $"{x}:")
                .ToList();

            var txsExcept = QueryTransactions().First().Value
                .Where(x => x.Category != null && !excluetopcategories.Contains(x.Category))
                .AsEnumerable()
                .Where(x => !excluetopcategoriesstartswith.Any(y => x.Category.StartsWith(y)))
                .AsQueryable<IReportable>();

            return new Query(txsExcept);
        }

        public Query QuerySplits(string top = null)
        {
            var splits = _context.Splits
                .Include(x => x.Transaction)
                .Where(x => x.Transaction.Timestamp.Year == Year && x.Transaction.Hidden != true && x.Transaction.Timestamp.Month <= Month)
                .ToList()
                .AsQueryable<IReportable>();

            if (top != null)
            {
                string ecolon = $"{top}:";
                splits = splits.Where(x => x.Category == top || x.Category.StartsWith(ecolon));
            }

            return new Query(splits);
        }

        public Query QuerySplitsExcept(IEnumerable<string> excluetopcategories)
        {
            var excluetopcategoriesstartswith = excluetopcategories
                .Select(x => $"{x}:")
                .ToList();

            var splitsExcept = QuerySplits().First().Value
                   .Where(x => !excluetopcategories.Contains(x.Category) && !excluetopcategoriesstartswith.Any(y => x.Category.StartsWith(y)))
                   .ToList()
                   .AsQueryable<IReportable>();

            return new Query(splitsExcept);
        }

        public Query QueryTransactionsComplete(string top = null)
        {
            return new Query(
                QueryTransactions(top),
                QuerySplits(top)
            );
        }

        public Query QueryTransactionsCompleteExcept(IEnumerable<string> tops)
        {
            return new Query(
                QueryTransactionsExcept(tops),
                QuerySplitsExcept(tops)
            );
        }

        public Query QueryBudget()
        {
            var budgettxs = _context.BudgetTxs
                .Where(x => x.Timestamp.Year == Year);

            return new Query(budgettxs);
        }

        public Query QueryBudgetExcept(IEnumerable<string> tops)
        {
            var topstarts = tops
                .Select(x => $"{x}:")
                .ToList();

            var budgetExcept = QueryBudget().First().Value
                .Where(x => x.Category != null && !tops.Contains(x.Category))
                .AsEnumerable()
                .Where(x => !topstarts.Any(y => x.Category.StartsWith(y)))
                .AsQueryable<IReportable>();

            return new Query(budgetExcept);
        }

        public Query QueryActualVsBudget()
        {
            return new Query
            (
                QueryTransactionsComplete().Labeled("Actual"),
                QueryBudget().Labeled("Budget")
            );
        }

        public Query QueryActualVsBudgetExcept(IEnumerable<string> tops)
        {
            return new Query
            (
                QueryTransactionsCompleteExcept(tops).Labeled("Actual"),
                QueryBudgetExcept(tops).Labeled("Budget")
            );
        }

        public Query QueryYearOverYear(out int[] years)
        {
            years = _context.Transactions.Select(x => x.Timestamp.Year).Distinct().ToArray();

            var yoy = new Query();
            foreach (var year in years)
            {
                var txsyear = _context.Transactions.Include(x => x.Splits).Where(x => x.Hidden != true && x.Timestamp.Year == year).Where(x => !x.Splits.Any());
                var splitsyear = _context.Splits.Include(x => x.Transaction).Where(x => x.Transaction.Hidden != true && x.Transaction.Timestamp.Year == year);

                yoy.Add(new KeyValuePair<string, IQueryable<IReportable>>(
                    year.ToString(),
                    txsyear
                ));
                yoy.Add(new KeyValuePair<string, IQueryable<IReportable>>(
                    year.ToString(),
                    splitsyear
                ));
            }

            return yoy;
        }

        public Report BuildReport(Parameters parms)
        {
            var result = new Report();

            Month = parms.month ?? 12;
            Year = parms.year ?? DateTime.Now.Year;

            var period = new DateTime(Year, Month, 1);
            result.Description = $"For {Year} through {period.ToString("MMMM")} ";
            
            var notexpenses = new List<string>() { "Savings", "Taxes", "Income", "Transfer", "Unmapped" };
            
            var budgetpctcolumn = new ColumnLabel()
            {
                Name = "% Progress",
                UniqueID = "Z",
                DisplayAsPercent = true,
                Custom = (cols) =>
                    cols.GetValueOrDefault("ID:Budget") == 0 ? 
                        0 : 
                        cols.GetValueOrDefault("ID:Actual") / cols.GetValueOrDefault("ID:Budget")
            };

            if (parms.id == "all")
            {
                result.WithMonthColumns = true;
                result.NumLevels = 2;
                result.Source = QueryTransactionsComplete();
                result.SortOrder = Report.SortOrders.TotalDescending;
                result.Name = "All Transactions";
            }
            else if (parms.id == "income")
            {
                result.Source = QueryTransactionsComplete(top: "Income");
                result.SkipLevels = 1;
                result.DisplayLevelAdjustment = 1; // Push levels up one when displaying
                result.SortOrder = Report.SortOrders.TotalAscending;
                result.Name = "Income";
            }
            else if (parms.id == "taxes")
            {
                result.Source = QueryTransactionsComplete(top: "Taxes");
                result.SkipLevels = 1;
                result.DisplayLevelAdjustment = 1; // Push levels up one when displaying
                result.SortOrder = Report.SortOrders.TotalDescending;
                result.Name = "Taxes";
            }
            else if (parms.id == "savings")
            {
                result.Source = QueryTransactionsComplete(top: "Savings");
                result.SkipLevels = 1;
                result.DisplayLevelAdjustment = 1; // Push levels up one when displaying
                result.SortOrder = Report.SortOrders.TotalDescending;
                result.Name = "Savings";
            }
            else if (parms.id == "expenses")
            {
                result.WithMonthColumns = true;
                result.Source = QueryTransactionsCompleteExcept(tops: notexpenses);
                result.NumLevels = 2;
                result.SortOrder = Report.SortOrders.TotalDescending;
                result.Name = "Expenses";
            }
            else if (parms.id == "expenses-budget")
            {
                result.Source = QueryBudgetExcept(tops: notexpenses);
                result.NumLevels = 3;
                result.SortOrder = Report.SortOrders.TotalDescending;
                result.Name = "Expenses Budget";
            }
            else if (parms.id == "expenses-v-budget")
            {
                result.AddCustomColumn(budgetpctcolumn);
                result.Source = QueryActualVsBudgetExcept(tops: notexpenses);
                result.WithTotalColumn = false;
                result.NumLevels = 3;
                result.SortOrder = Report.SortOrders.TotalDescending;
                result.Name = "Expenses vs. Budget";
            }
            else if (parms.id == "all-v-budget")
            {
                result.AddCustomColumn(budgetpctcolumn);
                result.Source = QueryActualVsBudget();
                result.WithTotalColumn = false;
                result.NumLevels = 3;
                result.SortOrder = Report.SortOrders.TotalDescending;
                result.Name = "All vs. Budget";
            }
            else if (parms.id == "budget")
            {
                result.NumLevels = 3;
                result.Source = QueryBudget();
                result.SortOrder = Report.SortOrders.TotalDescending;
                result.Description = $"For {Year}";
                result.Name = "Full Budget";
            }
            else if (parms.id == "yoy")
            {
                var years = new int[] { };
                result.Source = QueryYearOverYear(out years);
                result.Description = $"For {years.Min()} to {years.Max()}";
                result.NumLevels = 3;
                result.SortOrder = Report.SortOrders.TotalDescending;
                result.Name = "Year over Year";
            }
            else if (parms.id == "export")
            {
                result.Source = QueryActualVsBudget();
                result.LeafRowsOnly = true;
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
