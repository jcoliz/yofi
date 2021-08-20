using Microsoft.EntityFrameworkCore;
using YoFi.AspNet.Data;
using YoFi.AspNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.AspNet.Controllers.Reports
{
    /// <summary>
    /// This class has the unenviable task of building EF queries for
    /// the various scenarios we might want to report about, specific
    /// to this application's data.
    /// </summary>
    /// <remarks>
    /// The challenge is building queries that EF can translate later on
    /// when month/category groupings are built. Sometimes it can't be done
    /// in which case we'll turn it into a client-side query here.
    /// </remarks>
    public class QueryBuilder
    {
        private ApplicationDbContext _context;

        public QueryBuilder(ApplicationDbContext context)
        {
            _context = context;
        }

        public int Year { get; set; }
        public int Month { get; set; }

        private NamedQuery QueryTransactions(string top = null)
        {
            IQueryable<IReportable> txs = _context.Transactions
                .Where(x => x.Timestamp.Year == Year && x.Hidden != true && x.Timestamp.Month <= Month)
                .Where(x => !x.Splits.Any());

            if (top != null)
            {
                string ecolon = $"{top}:";
                txs = txs.Where(x => x.Category == top || x.Category.StartsWith(ecolon));
            }

            txs = txs
                .Select(x => new ReportableDto() { Amount = x.Amount, Timestamp = x.Timestamp, Category = x.Category });

            return new NamedQuery() { Query = txs };
        }

        /*
         * EF Core does a great job of the above. This is the final single query that it creates later
         * when doing GroupBy.
         *
            SELECT [t].[Category] AS [Name], DATEPART(month, [t].[Timestamp]) AS [Month], SUM([t].[Amount]) AS [Total]
            FROM [Transactions] AS [t]
            WHERE (((DATEPART(year, [t].[Timestamp]) = @__Year_0) AND (([t].[Hidden] <> CAST(1 AS bit)) OR [t].[Hidden] IS NULL)) AND (DATEPART(month, [t].[Timestamp]) <= @__Month_1)) AND NOT (EXISTS (
                SELECT 1
                FROM [Split] AS [s]
                WHERE [t].[ID] = [s].[TransactionID]))
            GROUP BY [t].[Category], DATEPART(month, [t].[Timestamp])
         */

        // TODO: Need to figure out how to get rid of the AsEnumerable() calls in the
        // "Except" pipelines. If I remove them, EF Core can't generate queries. So I will
        // have to study more on this.

        private NamedQuery QueryTransactionsExcept(IEnumerable<string> excludetopcategories)
        {
            var excludetopcategoriesstartswith = excludetopcategories
                .Select(x => $"{x}:")
                .ToList();

            var txsExcept = QueryTransactions().Query
                .Where(x => x.Category != null && !excludetopcategories.Contains(x.Category))
                .AsEnumerable()
                .Where(x => !excludetopcategoriesstartswith.Any(y => x.Category.StartsWith(y)))
                .AsQueryable<IReportable>();

            return new NamedQuery() { Query = txsExcept };
        }

        private NamedQuery QuerySplits(string top = null)
        {
            var splits = _context.Splits
                .Where(x => x.Transaction.Timestamp.Year == Year && x.Transaction.Hidden != true && x.Transaction.Timestamp.Month <= Month)
                .Include(x => x.Transaction)
                .Select(x=> new ReportableDto() { Amount = x.Amount, Timestamp = x.Transaction.Timestamp, Category = x.Category })
                .AsQueryable<IReportable>();

            if (top != null)
            {
                string ecolon = $"{top}:";
                splits = splits.Where(x => x.Category == top || x.Category.StartsWith(ecolon));
            }

            return new NamedQuery() { Query = splits };
        }

        /*
         * EF Core does a decent job of the above as well. It's straightforward because we have the ToList() here
         * which is needed for an of the Except* reports.
         * 
            SELECT [s].[Amount], [t].[Timestamp], [s].[Category]
            FROM [Split] AS [s]
            INNER JOIN [Transactions] AS [t] ON [s].[TransactionID] = [t].[ID]
            WHERE ((DATEPART(year, [t].[Timestamp]) = @__Year_0) AND (([t].[Hidden] <> CAST(1 AS bit)) OR [t].[Hidden] IS NULL)) AND (DATEPART(month, [t].[Timestamp]) <= @__Month_1)
         *
         * Leaving the ToList() out above works fine on All report, and generates the following
         * when doing GroupBy, which is perfect.
         * 
            SELECT [s].[Category] AS [Name], DATEPART(month, [t].[Timestamp]) AS [Month], SUM([s].[Amount]) AS [Total]
            FROM [Split] AS [s]
            INNER JOIN [Transactions] AS [t] ON [s].[TransactionID] = [t].[ID]
            WHERE ((DATEPART(year, [t].[Timestamp]) = @__Year_0) AND (([t].[Hidden] <> CAST(1 AS bit)) OR [t].[Hidden] IS NULL)) AND (DATEPART(month, [t].[Timestamp]) <= @__Month_1)
            GROUP BY [s].[Category], DATEPART(month, [t].[Timestamp])
         
         * Unfortunately, taking out the AsEnumerable() leads "QuerySplitsExcept" below to fail with a
         * "could not be translated" error.
         * 
         * OK so what I did was move the AsEnumerable() down to QuerySplitsExcept(), which is the only
         * place it was really needed.
         */

        private NamedQuery QuerySplitsExcept(IEnumerable<string> tops)
        {
            var excludetopcategoriesstartswith = tops
                .Select(x => $"{x}:")
                .ToList();

            var splitsExcept = QuerySplits().Query
                .Where(x => !tops.Contains(x.Category))
                .AsEnumerable()
                .Where(x => !excludetopcategoriesstartswith.Any(y => x.Category.StartsWith(y)))
                .AsQueryable<IReportable>();

            return new NamedQuery() { Query = splitsExcept };
        }

        public IEnumerable<NamedQuery> QueryTransactionsComplete(string top = null)
        {
            var txs = QueryTransactions(top).Query;
            var splits = QuerySplits(top).Query;
            var q = txs.Concat(splits);

            return new List<NamedQuery>() {
                new NamedQuery() { Query = q }
            };
        }

        /*
         * OK, I managed to concatenate the transactions and splits queries now, and hold it in EF Core all
         * the way to the end, which generates the following.
         * 
            SELECT [t1].[Category] AS [Name], DATEPART(month, [t1].[Timestamp]) AS [Month], SUM([t1].[Amount]) AS [Total]
            FROM (
                SELECT [t].[Amount], [t].[Timestamp], [t].[Category]
                FROM [Transactions] AS [t]
                WHERE (((DATEPART(year, [t].[Timestamp]) = @__Year_0) AND (([t].[Hidden] <> CAST(1 AS bit)) OR [t].[Hidden] IS NULL)) AND (DATEPART(month, [t].[Timestamp]) <= @__Month_1)) AND NOT (EXISTS (
                    SELECT 1
                    FROM [Split] AS [s]
                    WHERE [t].[ID] = [s].[TransactionID]))
                UNION ALL
                SELECT [s0].[Amount], [t0].[Timestamp], [s0].[Category]
                FROM [Split] AS [s0]
                INNER JOIN [Transactions] AS [t0] ON [s0].[TransactionID] = [t0].[ID]
                WHERE ((DATEPART(year, [t0].[Timestamp]) = @__Year_0) AND (([t0].[Hidden] <> CAST(1 AS bit)) OR [t0].[Hidden] IS NULL)) AND (DATEPART(month, [t0].[Timestamp]) <= @__Month_1)
            ) AS [t1]
            GROUP BY [t1].[Category], DATEPART(month, [t1].[Timestamp])
         *
         * I will still need to figure out how to do the same on the "except" queries.
         */

        public IEnumerable<NamedQuery> QueryTransactionsCompleteExcept(IEnumerable<string> tops)
        {
            var txs = QueryTransactionsExcept(tops).Query;
            var splits = QuerySplitsExcept(tops).Query;
            var q = txs.Concat(splits);

            return new List<NamedQuery>() {
                new NamedQuery() { Query = q }
            };
        }

        private NamedQuery QueryBudgetSingle()
        {
            // Maybe don't need a DTO for BudgetTx, because they don't have any extra fields?
            var budgettxs = _context.BudgetTxs
                .Where(x => x.Timestamp.Year == Year)
                .Select(x => new ReportableDto() { Amount = x.Amount, Timestamp = x.Timestamp, Category = x.Category });

            return new NamedQuery() { Query = budgettxs };
        }

        public IEnumerable<NamedQuery> QueryBudget() => new List<NamedQuery>() { QueryBudgetSingle() };

        private NamedQuery QueryBudgetSingleExcept(IEnumerable<string> tops)
        {
            var topstarts = tops
                .Select(x => $"{x}:")
                .ToList();

            var budgetExcept = QueryBudgetSingle().Query
                .Where(x => x.Category != null && !tops.Contains(x.Category))
                .AsEnumerable()
                .Where(x => !topstarts.Any(y => x.Category.StartsWith(y)))
                .AsQueryable<IReportable>();

            return new NamedQuery() { Query = budgetExcept };
        }
        public IEnumerable<NamedQuery> QueryBudgetExcept(IEnumerable<string> tops) => new List<NamedQuery>() { QueryBudgetSingleExcept(tops) };

        public IEnumerable<NamedQuery> QueryActualVsBudget(bool leafrows = false)
        {
            var result = new List<NamedQuery>();

            result.AddRange(QueryTransactionsComplete().Select(x => x.Labeled("Actual").AsLeafRowsOnly(leafrows)));
            result.Add(QueryBudgetSingle().Labeled("Budget").AsLeafRowsOnly(leafrows));

            return result;
        }

        private NamedQuery QueryManagedBudgetSingle()
        {
            // Start with the usual transactions
            var budgettxs = QueryBudgetSingle().Query;

            // "Managed" Categories are those with more than one budgettx in a year.
            var categories = budgettxs.GroupBy(x => x.Category).Select(g => new { Key = g.Key, Count = g.Count() }).Where(x => x.Count > 1).Select(x => x.Key);
            var managedtxs = budgettxs.Where(x => x.Timestamp.Month <= Month && categories.Contains(x.Category));

#if false
            foreach (var it in managedtxs.ToList())
                Console.WriteLine($"{it.Timestamp} {it.Category} {it.Amount}");
#endif

            return new NamedQuery() { Query = managedtxs, LeafRowsOnly = true };
        }

        /*
         * Here's the query for above:
         * 
            SELECT [b].[Category] AS [Name], SUM([b].[Amount]) AS [Total]
            FROM [BudgetTxs] AS [b]
            WHERE (DATEPART(year, [b].[Timestamp]) = @__Year_0) AND ((DATEPART(month, [b].[Timestamp]) <= @__Month_1) AND [b].[Category] IN (
                SELECT [b0].[Category]
                FROM [BudgetTxs] AS [b0]
                WHERE DATEPART(year, [b0].[Timestamp]) = @__Year_0
                GROUP BY [b0].[Category]
                HAVING COUNT(*) > 1
            )
            )
            GROUP BY [b].[Category]
         */

        public IEnumerable<NamedQuery> QueryManagedBudget()
        {
            var result = new List<NamedQuery>();

            result.Add(QueryManagedBudgetSingle().Labeled("Budget"));
            result.AddRange(QueryTransactionsComplete().Select(x => x.Labeled("Actual")));

            return result;
        }

        public IEnumerable<NamedQuery> QueryActualVsBudgetExcept(IEnumerable<string> tops)
        {
            var result = new List<NamedQuery>();

            result.AddRange(QueryTransactionsCompleteExcept(tops).Select(x => x.Labeled("Actual")));
            result.Add(QueryBudgetSingleExcept(tops).Labeled("Budget"));

            return result;
        }

        public IEnumerable<NamedQuery> QueryYearOverYear(out int[] years)
        {
            var result = new List<NamedQuery>();

            years = _context.Transactions.Select(x => x.Timestamp.Year).Distinct().ToArray();
            foreach (var year in years)
            {
                var txsyear = _context.Transactions.Include(x => x.Splits).Where(x => x.Hidden != true && x.Timestamp.Year == year).Where(x => !x.Splits.Any());
                var splitsyear = _context.Splits.Include(x => x.Transaction).Where(x => x.Transaction.Hidden != true && x.Transaction.Timestamp.Year == year);

                result.Add(new NamedQuery() { Name = year.ToString(), Query = txsyear });
                result.Add(new NamedQuery() { Name = year.ToString(), Query = splitsyear });
            }

            return result;
        }
    }

    /// <summary>
    /// Data transfer object for report data
    /// </summary>
    /// <remarks>
    /// Implements the minimum needed to present the IReportable
    /// interface, which is the most we should ever be fetching from
    /// DB for reports
    /// </remarks>
    public class ReportableDto : IReportable
    {
        public decimal Amount { get; set; }

        public DateTime Timestamp { get; set; }

        public string Category { get; set; }
    }

}
