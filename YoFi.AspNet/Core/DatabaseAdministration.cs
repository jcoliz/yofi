using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core
{
    public class DatabaseAdministration : IDatabaseAdministration
    {
        public const string TestMarker = "__test__";

        private readonly IDataContext _context;

        public DatabaseAdministration(IDataContext context)
        {
            this._context = context;
        }

        public async Task ClearDatabaseAsync(string id)
        {
            if ("budget" == id)
            {
                // TODO: Async() ??
                _context.RemoveRange(_context.BudgetTxs);
                await _context.SaveChangesAsync();
            }
            else if ("tx" == id)
            {
                _context.RemoveRange(_context.TransactionsWithSplits);
                await _context.SaveChangesAsync();
            }
            else if ("payee" == id)
            {
                _context.RemoveRange(_context.Payees);
                await _context.SaveChangesAsync();
            }
            else
            {
                // TODO: Return an error
            }
        }

        public async Task ClearTestDataAsync(string id)
        {
            if (id.Contains("payee") || "all" == id)
                _context.RemoveRange(_context.Payees.Where(x => x.Category.Contains(TestMarker)));

            if (id.Contains("budgettx") || "all" == id)
                _context.RemoveRange(_context.BudgetTxs.Where(x => x.Category.Contains(TestMarker)));

            if (id.Contains("trx") || "all" == id)
            {
                _context.RemoveRange(_context.Transactions.Where(x => x.Category.Contains(TestMarker) || x.Memo.Contains(TestMarker) || x.Payee.Contains(TestMarker)));
                _context.RemoveRange(_context.Splits.Where(x => x.Category.Contains(TestMarker)));
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IDatabaseStatus> GetDatabaseStatus()
        {
            var result = new DatabaseStatus();
            result.NumTransactions = await _context.CountAsync(_context.Transactions);
            result.NumBudgetTxs = await _context.CountAsync(_context.BudgetTxs);
            result.NumPayees = await _context.CountAsync(_context.Payees);
            result.IsEmpty = result.NumTransactions == 0 && result.NumBudgetTxs == 0 && result.NumPayees == 0;

            return result;
        }
    }

    public class DatabaseStatus : IDatabaseStatus
    {
        public bool IsEmpty { get; set; }

        public int NumTransactions { get; set; }

        public int NumBudgetTxs { get; set; }

        public int NumPayees { get; set; }
    }
}
