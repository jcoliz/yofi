using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.Core
{
    public interface IDataContext
    {
        IQueryable<Transaction> Transactions { get; }
        IQueryable<Split> Splits { get; }
        IQueryable<Payee> Payees { get; }
        IQueryable<BudgetTx> BudgetTxs { get; }
        IQueryable<Transaction> TransactionsWithSplits { get; }
        IQueryable<Split> SplitsWithTransactions { get; }
        IQueryable<T> Get<T>();

        Task AddAsync(object item);
        Task AddRangeAsync(IEnumerable<object> items);

        void Update(object item);
        void Remove(object item);
        void RemoveRange(IEnumerable<object> items);
        Task SaveChangesAsync();

        // Async Query methods

        Task ToListAsync<T>(IQueryable<T> query);
    }
}
