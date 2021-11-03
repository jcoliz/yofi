using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core;
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

        public override IQueryable<Transaction> ForQuery(string q) => string.IsNullOrEmpty(q) ? OrderedQuery : OrderedQuery.Where(x => x.Category.Contains(q) || x.Payee.Contains(q));
    }
}
