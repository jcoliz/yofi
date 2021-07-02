using Microsoft.EntityFrameworkCore;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Controllers.Reports
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

        private NamedQuery QuerySplits(string top = null)
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

            return new Query() { new NamedQuery() { Query = budgettxs } };
        }

        public Query QueryBudgetExcept(IEnumerable<string> tops)
        {
            var topstarts = tops
                .Select(x => $"{x}:")
                .ToList();

            var budgetExcept = QueryBudget().First().Query
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

                yoy.Add(new NamedQuery() { Name = year.ToString(), Query = txsyear });
                yoy.Add(new NamedQuery() { Name = year.ToString(), Query = splitsyear });
            }

            return yoy;
        }

    }
}
