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

        // NOTE: These ToList()'s and AsEnumerable()'s have been added wherever Entity Framework couldn't build queries
        // in the report generator. AsEnumerable() is preferred, as it defers execution. However in some cases, THAT 
        // doesn't even work, so I needed ToList().

        private NamedQuery QueryTransactions(string top = null)
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

            return new NamedQuery() { Query = txs };
        }

        private NamedQuery QueryTransactionsExcept(IEnumerable<string> excluetopcategories)
        {
            var excluetopcategoriesstartswith = excluetopcategories
                .Select(x => $"{x}:")
                .ToList();

            var txsExcept = QueryTransactions().Query
                .Where(x => x.Category != null && !excluetopcategories.Contains(x.Category))
                .AsEnumerable()
                .Where(x => !excluetopcategoriesstartswith.Any(y => x.Category.StartsWith(y)))
                .AsQueryable<IReportable>();

            return new NamedQuery() { Query = txsExcept };
        }

        public class ReportableDto : IReportable
        {
            public decimal Amount { get; set; }

            public DateTime Timestamp { get; set; }

            public string Category { get; set; }
        }

        private NamedQuery QuerySplits(string top = null)
        {
            var splits = _context.Splits
                .Where(x => x.Transaction.Timestamp.Year == Year && x.Transaction.Hidden != true && x.Transaction.Timestamp.Month <= Month)
                .Include(x => x.Transaction)
                .Select(x=> new ReportableDto() { Amount = x.Amount, Timestamp = x.Transaction.Timestamp, Category = x.Category })
                .ToList()
                .AsQueryable<IReportable>();

            if (top != null)
            {
                string ecolon = $"{top}:";
                splits = splits.Where(x => x.Category == top || x.Category.StartsWith(ecolon));
            }

            return new NamedQuery() { Query = splits };
        }

        private NamedQuery QuerySplitsExcept(IEnumerable<string> tops)
        {
            var excluetopcategoriesstartswith = tops
                .Select(x => $"{x}:")
                .ToList();

            var splitsExcept = QuerySplits().Query
                   .Where(x => !tops.Contains(x.Category) && !excluetopcategoriesstartswith.Any(y => x.Category.StartsWith(y)))
                   .ToList()
                   .AsQueryable<IReportable>();

            return new NamedQuery() { Query = splitsExcept };
        }

        public IEnumerable<NamedQuery> QueryTransactionsComplete(string top = null)
        {
            return new List<NamedQuery>() {
                QueryTransactions(top),
                QuerySplits(top)
            };
        }

        public IEnumerable<NamedQuery> QueryTransactionsCompleteExcept(IEnumerable<string> tops)
        {
            return new List<NamedQuery>() {
                QueryTransactionsExcept(tops),
                QuerySplitsExcept(tops)
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

            // This is SO not going to translate into a query :O
            var managedtxs = budgettxs.Where(x => x.Timestamp.Month <= Month && categories.Contains(x.Category));

            // JUST FOR FUN!!
            var runit = managedtxs.ToList();
            foreach (var it in runit)
                Console.WriteLine($"{it.Timestamp} {it.Category} {it.Amount}");

            return new NamedQuery() { Query = managedtxs, LeafRowsOnly = true };
        }

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
}
