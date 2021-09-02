using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using YoFi.AspNet.Data;
using YoFi.AspNet.Models;

namespace YoFi.AspNet.Controllers.Reports
{
    /// <summary>
    /// Build EF Core queries for app-specific scenarios.
    /// </summary>
    /// <remarks>
    /// This class has the unenviable task of building EF queries for
    /// the various scenarios we might want to report about, specific
    /// to this application's data.
    /// 
    /// The challenge is building queries that EF can translate later on
    /// when month/category groupings are built. Sometimes it can't be done
    /// in which case we'll turn it into a client-side query here.
    /// 
    /// Note that all public methods return an IEnumerable(NamedQuery) because
    /// this is what the Report class expects as a source, even if there is only
    /// a single query in it.
    /// </remarks>
    public class QueryBuilder
    {
        #region Properties

        /// <summary>
        /// Which year does this report cover
        /// </summary>
        /// <remarks>
        /// Almost all reports are constrained to a single year
        /// </remarks>
        public int Year { get; set; }

        /// <summary>
        /// How many months into the year should be included
        /// </summary>
        /// <remarks>
        /// Most reports are "year to date" reports. This tell us, "to which date?"
        /// </remarks>
        public int Month { get; set; }

        #endregion

        #region Constructor(s)
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">Which database context to create queries against</param>
        public QueryBuilder(ApplicationDbContext context)
        {
            _context = context;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Generate queries for transactions AND splits
        /// </summary>
        /// <param name="top">Optional limiter. If set, will only include items with this top category</param>
        /// <param name="excluded">Optional limited. If set, will excluded items in these top categories</param>
        /// <returns>Resulting queries</returns>
        public IEnumerable<NamedQuery> QueryTransactionsComplete(string top = null, IEnumerable<string> excluded = null)
        {
            IQueryable<IReportable> txs = null;
            IQueryable<IReportable> splits = null;

            if (excluded?.Any() == true)
            {
                if (!string.IsNullOrEmpty(top))
                    throw new ArgumentException("Cannot set top and excluded in the same query");

                txs = QueryTransactionsExcept(excluded).Query;
                splits = QuerySplitsExcept(excluded).Query;
            }
            else
            {
                txs = QueryTransactions(top).Query;
                splits = QuerySplits(top).Query;
            }

            return new List<NamedQuery>() {
                new NamedQuery()
                {
                    Query = txs.Concat(splits)
                }
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

        /// <summary>
        /// Generate queries for budget line items
        /// </summary>
        /// <remarks>
        /// Report ultimately needs an enumerable of queries, so this method puts it in the right format
        /// for the Report.
        /// </remarks>
        /// <see cref="Report"/>
        /// <returns>Resulting queries</returns>
        public IEnumerable<NamedQuery> QueryBudget() => new List<NamedQuery>() { QueryBudgetSingle() };

        /// <summary>
        /// Generate queries for budget line items, except those with <paramref name="excluded"/> top categories
        /// </summary>
        /// <param name="excluded">Which top categories to exclude</param>
        /// <remarks>
        /// Report ultimately needs an enumerable of queries, so this method puts it in the right format
        /// for the Report.
        /// </remarks>
        /// <see cref="Report"/>
        /// <returns>Resulting queries</returns>
        public IEnumerable<NamedQuery> QueryBudgetExcept(IEnumerable<string> excluded) => new List<NamedQuery>() { QueryBudgetSingleExcept(excluded) };

        /// <summary>
        /// Generate queries for transactions & splits compared to budget line items
        /// </summary>
        /// <param name="leafrows">Whether to include only the leaf row. 'false' will also include summary headings</param>
        /// <returns>Resulting queries</returns>
        public IEnumerable<NamedQuery> QueryActualVsBudget(bool leafrows = false)
        {
            var result = new List<NamedQuery>();

            result.AddRange(QueryTransactionsComplete().Select(x => x.Labeled("Actual").AsLeafRowsOnly(leafrows)));
            result.Add(QueryBudgetSingle().Labeled("Budget").AsLeafRowsOnly(leafrows));

            return result;
        }

        /// <summary>
        /// Generate a query for "managed" budget line items vs. transactions and splits
        /// </summary>
        /// <remarks>
        /// "Managed", here means we manage them more carefully because they have monthly or weekly budget
        /// amounts, not just a single annual amount like 'unmanaged' budget line items
        /// </remarks>
        /// <returns>Resulting queries</returns>
        public IEnumerable<NamedQuery> QueryManagedBudget()
        {
            var result = new List<NamedQuery>();

            // "Budget" items need to go in first, because they will control which rows show up in the
            // final report
            result.Add(QueryManagedBudgetSingle().Labeled("Budget"));
            result.AddRange(QueryTransactionsComplete().Select(x => x.Labeled("Actual")));

            return result;
        }

        /// <summary>
        /// Generate queries for transactions & splits compared to budget line items, excluding those 
        /// with <paramref name="excluded"/> top categories
        /// </summary>
        /// <param name="excluded">Which top categories to exclude</param>
        /// <returns>Resulting queries</returns>
        public IEnumerable<NamedQuery> QueryActualVsBudgetExcept(IEnumerable<string> excluded)
        {
            var result = new List<NamedQuery>();

            result.AddRange(QueryTransactionsComplete(excluded:excluded).Select(x => x.Labeled("Actual")));
            result.Add(QueryBudgetSingleExcept(excluded).Labeled("Budget"));

            return result;
        }

        /// <summary>
        /// Generate queries for a year-over-year report, comparing multiple <paramref name="years"/> of data
        /// </summary>
        /// <param name="years">Which years to include</param>
        /// <returns>Resulting queries</returns>
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

        #endregion

        #region Fields
        private ApplicationDbContext _context;
        #endregion

        #region Internals

        /// <summary>
        /// Generate a query for transactions, optionally limited to only those with given <paramref name="top"/> category
        /// </summary>
        /// <param name="top">Optional limiter. If set, will only include items with this top category</param>
        /// <returns>Resulting query</returns>
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

        /// <summary>
        /// Generate a query for transactions, except those with <paramref name="excluded"/> top categories
        /// </summary>
        /// <param name="excluded">Which top categories to exclude</param>
        /// <returns>Resulting query</returns>
        private NamedQuery QueryTransactionsExcept(IEnumerable<string> excluded)
        {
            var excludetopcategoriesstartswith = excluded
                .Select(x => $"{x}:")
                .ToList();

            var txsExcept = QueryTransactions().Query
                .Where(x => x.Category != null && !excluded.Contains(x.Category))
                .AsEnumerable()
                .Where(x => !excludetopcategoriesstartswith.Any(y => x.Category.StartsWith(y)))
                .AsQueryable<IReportable>();

            return new NamedQuery() { Query = txsExcept };
        }

        /// <summary>
        /// Generate a query for splits, optionally limited to only those with given <paramref name="top"/> category
        /// </summary>
        /// <param name="top">Optional limiter. If set, will only include items with this top category</param>
        /// <returns>Resulting query</returns>
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
         * EF Core does a decent job of the above as well. It's straightforward because we have the AsQueryable() here
         * which is needed for an of the Except* reports.
         * 
            SELECT [s].[Amount], [t].[Timestamp], [s].[Category]
            FROM [Split] AS [s]
            INNER JOIN [Transactions] AS [t] ON [s].[TransactionID] = [t].[ID]
            WHERE ((DATEPART(year, [t].[Timestamp]) = @__Year_0) AND (([t].[Hidden] <> CAST(1 AS bit)) OR [t].[Hidden] IS NULL)) AND (DATEPART(month, [t].[Timestamp]) <= @__Month_1)
         *
         * Leaving the AsQueryable() out above works fine on All report, and generates the following
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

        /// <summary>
        /// Generate a query for splits, excluding those with <paramref name="excluded"/> top categories
        /// </summary>
        /// <param name="excluded">Which top categories to exclude</param>
        /// <returns>Resulting query</returns>
        private NamedQuery QuerySplitsExcept(IEnumerable<string> excluded)
        {
            var excludetopcategoriesstartswith = excluded
                .Select(x => $"{x}:")
                .ToList();

            var splitsExcept = QuerySplits().Query
                .Where(x => !excluded.Contains(x.Category))
                .AsEnumerable()
                .Where(x => !excludetopcategoriesstartswith.Any(y => x.Category.StartsWith(y)))
                .AsQueryable<IReportable>();

            return new NamedQuery() { Query = splitsExcept };
        }

        /// <summary>
        /// Generate a query for budget line items
        /// </summary>
        /// <returns>Resulting query</returns>
        private NamedQuery QueryBudgetSingle()
        {
            // Note that using a DTO for budget line items protects against overfetching in the future if we add new
            // fields to that object which are not used for reports.
            var budgettxs = _context.BudgetTxs
                .Where(x => x.Timestamp.Year == Year)
                .Select(x => new ReportableDto() { Amount = x.Amount, Timestamp = x.Timestamp, Category = x.Category });

            return new NamedQuery() { Query = budgettxs };
        }

        /// <summary>
        /// Generate a query for budget line items, except those with <paramref name="excluded"/> top categories
        /// </summary>
        /// <param name="excluded">Which top categories to exclude</param>
        /// <returns>Resulting query</returns>
        private NamedQuery QueryBudgetSingleExcept(IEnumerable<string> excluded)
        {
            var topstarts = excluded
                .Select(x => $"{x}:")
                .ToList();

            var budgetExcept = QueryBudgetSingle().Query
                .Where(x => x.Category != null && !excluded.Contains(x.Category))
                .AsEnumerable()
                .Where(x => !topstarts.Any(y => x.Category.StartsWith(y)))
                .AsQueryable<IReportable>();

            return new NamedQuery() { Query = budgetExcept };
        }

        /// <summary>
        /// Generate a query for "managed" budget line items
        /// </summary>
        /// <remarks>
        /// "Managed", here means we manage them more carefully because they have monthly or weekly budget
        /// amounts, not just a single annual amount like 'unmanaged' budget line items
        /// </remarks>
        /// <returns>Resulting query</returns>
        private NamedQuery QueryManagedBudgetSingle()
        {
            // Start with the usual transactions
            var budgettxs = QueryBudgetSingle().Query;

            // "Managed" Categories are those with more than one budgettx in a year.
            var categories = budgettxs.GroupBy(x => x.Category).Select(g => new { Key = g.Key, Count = g.Count() }).Where(x => x.Count > 1).Select(x => x.Key);
            var managedtxs = budgettxs.Where(x => x.Timestamp.Month <= Month && categories.Contains(x.Category));

#if false
            // Review the results for debugging
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

        #endregion
    }

    /// <summary>
    /// Data transfer object for report data
    /// </summary>
    /// <remarks>
    /// Implements the minimum needed to present the IReportable
    /// interface, which is the most we should ever be fetching from
    /// DB for reports
    /// 
    /// In the future, when all queries are passed all the way
    /// through and only executed once, this may not be needed.
    /// That's because the final query selects only what it needs.
    /// However, currently there are intermediate AsEnumerable()
    /// calls which we want to protect from over-fetching.
    /// </remarks>
    class ReportableDto : IReportable
    {
        public decimal Amount { get; set; }

        public DateTime Timestamp { get; set; }

        public string Category { get; set; }
    }
}
