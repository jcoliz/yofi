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

        Task AddRangeAsync(IEnumerable<object> incoming);
        Task SaveChangesAsync();
    }
}
