using Microsoft.EntityFrameworkCore;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Controllers.Helpers
{
    public class Query : List<KeyValuePair<string, IQueryable<IReportable>>>
    {
        public Query() { }
        public Query(IQueryable<IReportable> single)
        {
            Add(new KeyValuePair<string, IQueryable<IReportable>>(string.Empty, single));
        }
        public Query(params Query[] many)
        {
            foreach(var q in many)
                AddRange(q);
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

        public Query QueryTransactions(string topcategory = null)
        {
            var txs = _context.Transactions
                .Include(x => x.Splits)
                .Where(x => x.Timestamp.Year == Year && x.Hidden != true && x.Timestamp.Month <= Month)
                .Where(x => !x.Splits.Any());

            if (topcategory != null)
            {
                string ecolon = $"{topcategory}:";
                txs = txs.Where(x => x.Category == topcategory || x.Category.StartsWith(ecolon));
            }

            return new Query(txs);
        }

        public Query QuerySplits(string topcategory = null)
        {
            var splits = _context.Splits
                .Include(x => x.Transaction)
                .Where(x => x.Transaction.Timestamp.Year == Year && x.Transaction.Hidden != true && x.Transaction.Timestamp.Month <= Month)
                .ToList()
                .AsQueryable<IReportable>();

            if (topcategory != null)
            {
                string ecolon = $"{topcategory}:";
                splits = splits.Where(x => x.Category == topcategory || x.Category.StartsWith(ecolon));
            }

            return new Query(splits);
        }

        public Query QueryTransactionsComplete(string topcategory = null)
        {
            return new Query(QueryTransactions(topcategory),QuerySplits(topcategory));
        }

        public Report BuildReport(Parameters parms)
        {
            var result = new Report();

            Month = parms.month ?? 12;
            Year = parms.year ?? DateTime.Now.Year;

            var period = new DateTime(Year, Month, 1);
            result.Description = $"For {Year} through {period.ToString("MMMM")} ";

            // NOTE: These ToList()'s and AsEnumerable()'s have been added wherever Entity Framework couldn't build queries
            // in the report generator. AsEnumerable() is preferred, as it defers execution. However in some cases, THAT 
            // doesn't even work, so I needed ToList().
            //
            // TODO: Should avoid executing these ToList operations here, because it could needlessly take time. However,
            // MOST queries require at least the splits ToList(), so it is more rare that it's not needed.

            var txs = _context.Transactions
                .Include(x => x.Splits)
                .Where(x => x.Timestamp.Year == Year && x.Hidden != true && x.Timestamp.Month <= Month)
                .Where(x => !x.Splits.Any());

            var splits = _context.Splits
                .Include(x => x.Transaction)
                .Where(x => x.Transaction.Timestamp.Year == Year && x.Transaction.Hidden != true && x.Transaction.Timestamp.Month <= Month)
                .ToList()
                .AsQueryable<IReportable>();
            
            var budgettxs = _context.BudgetTxs
                .Where(x => x.Timestamp.Year == Year);
            
            var excludeExpenses = new List<string>() { "Savings", "Taxes", "Income", "Transfer", "Unmapped" };
            
            var excludestartsExpenses = excludeExpenses
                .Select(x => $"{x}:")
                .ToList();
            
            var txsExpenses = txs
                .Where(x => x.Category != null && !excludeExpenses.Contains(x.Category))
                .AsEnumerable()
                .Where(x => !excludestartsExpenses.Any(y => x.Category.StartsWith(y)))
                .AsQueryable<IReportable>(); 
            
            var splitsExpenses = splits
                .Where(x => !excludeExpenses.Contains(x.Category) && !excludestartsExpenses.Any(y => x.Category.StartsWith(y)))
                .ToList()
                .AsQueryable<IReportable>();
            
            var budgettxsExpenses = budgettxs
                .Where(x => x.Category != null && !excludeExpenses.Contains(x.Category))
                .AsEnumerable()
                .Where(x => !excludestartsExpenses.Any(y => x.Category.StartsWith(y)))
                .AsQueryable<IReportable>();

            var txscomplete = new List<IQueryable<IReportable>>()
            {
                txs, splits
            };

            var txscompleteExpenses = new List<IQueryable<IReportable>>()
            {
                txsExpenses, splitsExpenses
            };

            var serieslistexpenses = new List<KeyValuePair<string, IQueryable<IReportable>>>()
            {
                new KeyValuePair<string, IQueryable<IReportable>>("Actual", txsExpenses),
                new KeyValuePair<string, IQueryable<IReportable>>("Actual", splitsExpenses),
                new KeyValuePair<string, IQueryable<IReportable>>("Budget", budgettxsExpenses),
            };

            var serieslistall = new List<KeyValuePair<string, IQueryable<IReportable>>>()
            {
                new KeyValuePair<string, IQueryable<IReportable>>("Actual", txs),
                new KeyValuePair<string, IQueryable<IReportable>>("Actual", splits),
                new KeyValuePair<string, IQueryable<IReportable>>("Budget", budgettxs),
            };

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

            Func<string, IEnumerable<IQueryable<IReportable>>> txsplitsfor = e =>
            {
                string ecolon = $"{e}:";
                return new List<IQueryable<IReportable>>()
                {
                    txs.Where(x => x.Category == e || x.Category.StartsWith(ecolon)),
                    splits.Where(x => x.Category == e || x.Category.StartsWith(ecolon))
                };
            };

            if (parms.id == "all")
            {
                result.WithMonthColumns = true;
                result.NumLevels = 2;
                result.MultipleSources = QueryTransactionsComplete();
                result.SortOrder = Helpers.Report.SortOrders.TotalDescending;
                result.Name = "All Transactions";
            }
            else if (parms.id == "income")
            {
                result.MultipleSources = QueryTransactionsComplete("Income");
                result.SkipLevels = 1;
                result.DisplayLevelAdjustment = 1; // Push levels up one when displaying
                result.SortOrder = Helpers.Report.SortOrders.TotalAscending;
                result.Name = "Income";
            }
            else if (parms.id == "taxes")
            {
                result.MultipleSources = QueryTransactionsComplete("Taxes");
                result.SkipLevels = 1;
                result.DisplayLevelAdjustment = 1; // Push levels up one when displaying
                result.SortOrder = Helpers.Report.SortOrders.TotalDescending;
                result.Name = "Taxes";
            }
            else if (parms.id == "savings")
            {
                result.MultipleSources = QueryTransactionsComplete("Savings");
                result.SkipLevels = 1;
                result.DisplayLevelAdjustment = 1; // Push levels up one when displaying
                result.SortOrder = Helpers.Report.SortOrders.TotalDescending;
                result.Name = "Savings";
            }
            else if (parms.id == "expenses")
            {
                result.WithMonthColumns = true;
                result.SingleSourceList = txscompleteExpenses;
                result.NumLevels = 2;
                result.SortOrder = Helpers.Report.SortOrders.TotalDescending;
                result.Name = "Expenses";
            }
            else if (parms.id == "expenses-budget")
            {
                result.SingleSource = budgettxsExpenses.AsQueryable<IReportable>();
                result.NumLevels = 3;
                result.SortOrder = Helpers.Report.SortOrders.TotalDescending;
                result.Name = "Expenses Budget";
            }
            else if (parms.id == "expenses-v-budget")
            {
                result.AddCustomColumn(budgetpctcolumn);
                result.MultipleSources = serieslistexpenses;
                result.WithTotalColumn = false;
                result.NumLevels = 3;
                result.SortOrder = Helpers.Report.SortOrders.TotalDescending;
                result.Name = "Expenses vs. Budget";
            }
            else if (parms.id == "all-v-budget")
            {
                result.AddCustomColumn(budgetpctcolumn);
                result.MultipleSources = serieslistall;
                result.WithTotalColumn = false;
                result.NumLevels = 3;
                result.SortOrder = Helpers.Report.SortOrders.TotalDescending;
                result.Name = "All vs. Budget";
            }
            else if (parms.id == "budget")
            {
                result.NumLevels = 3;
                result.SingleSource = budgettxs.AsQueryable<IReportable>();
                result.SortOrder = Helpers.Report.SortOrders.TotalDescending;
                result.Description = $"For {Year}";
                result.Name = "Full Budget";
            }
            else if (parms.id == "yoy")
            {
                var years = _context.Transactions.Select(x=>x.Timestamp.Year).Distinct().ToList();

                var yoy = new List<KeyValuePair<string, IQueryable<IReportable>>>();
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

                result.Description = $"For {years.Min()} to {years.Max()}";
                result.NumLevels = 3;
                result.SortOrder = Helpers.Report.SortOrders.TotalDescending;
                result.Name = "Year over Year";

                result.MultipleSources = yoy;
            }
            else if (parms.id == "export")
            {
                result.MultipleSources = serieslistall;
                result.LeafRowsOnly = true;
                result.WithTotalColumn = false;
                result.NumLevels = 4;
                result.SortOrder = Helpers.Report.SortOrders.NameAscending;
                result.Name = "Transaction Export";
            }

            if (parms.level.HasValue)
            {
                result.NumLevels = parms.level.Value;
                if (result.NumLevels == 1)
                    result.DisplayLevelAdjustment = 1;
            }

            if (parms.showmonths.HasValue)
            {
                result.WithMonthColumns = parms.showmonths.Value;
            }

            result.Build();
            result.WriteToConsole();

            return result;
        }
   }
}
