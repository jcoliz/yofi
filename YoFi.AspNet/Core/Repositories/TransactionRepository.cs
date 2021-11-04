using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core;
using YoFi.Core.Helpers;
using YoFi.Core.Models;

namespace YoFi.Core.Repositories
{
    public class TransactionRepository : BaseRepository<Transaction>, ITransactionRepository
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">Where to find the data we actually contain</param>
        public TransactionRepository(IDataContext context) : base(context)
        {
        }

        public async Task<bool> AssignPayeeAsync(Transaction transaction) => await new PayeeMatcher(_context).SetCategoryBasedOnMatchingPayeeAsync(transaction);

        public override IQueryable<Transaction> ForQuery(string q) => string.IsNullOrEmpty(q) ? OrderedQuery : OrderedQuery.Where(x => x.Category.Contains(q) || x.Payee.Contains(q));

        // TODO: SingleAsync()
        public Task<Transaction> GetWithSplitsByIdAsync(int? id) => Task.FromResult(_context.TransactionsWithSplits.Single(x => x.ID == id.Value));
    }
}
