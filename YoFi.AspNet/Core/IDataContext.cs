using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Models;

namespace YoFi.Core
{
    public interface IDataContext
    {
        IQueryable<Transaction> Transactions { get; }
        IQueryable<Split> Splits { get; }
        IQueryable<Payee> Payees { get; }
        IQueryable<BudgetTx> BudgetTxs { get; }

        Task AddAsync(object item);
        Task AddRangeAsync(IEnumerable<object> items);

        void Update(object item);
        void Remove(object item);
        Task SaveChangesAsync();
    }
}
