using System;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core;

namespace YoFi.Core.Repositories
{
    /// <summary>
    /// Provides access to Payee items, along with 
    /// domain-specific business logic unique to Payee items
    /// </summary>

    public class PayeeRepository : BaseRepository<Payee>, IPayeeRepository
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">Where to find the data we actually contain</param>
        public PayeeRepository(IDataContext context) : base(context)
        {
        }

        /// <summary>
        /// Subset of all known items reduced by the specified query parameter
        /// </summary>
        /// <param name="q">Query describing the desired subset</param>
        /// <returns>Requested items</returns>
        public override IQueryable<Payee> ForQuery(string q) => string.IsNullOrEmpty(q) ? OrderedQuery : OrderedQuery.Where(x => x.Category.Contains(q) || x.Name.Contains(q));

        /// <summary>
        /// Change category of all selected items to <paramref name="category"/>
        /// </summary>
        /// <param name="category">Next category</param>
        public async Task BulkEdit(string category)
        {
            foreach (var item in All.Where(x => x.Selected == true))
            {
                if (!string.IsNullOrEmpty(category))
                    item.Category = category;

                item.Selected = false;
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Remove all selected items from the database
        /// </summary>
        public async Task BulkDelete()
        {
            _context.RemoveRange(All.Where(x => x.Selected == true));
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Create a new payee based upon the supplied transaction (by id)
        /// </summary>
        /// <remarks>
        /// The idea is that the resulting payee matching rule would match the
        /// given transaction, and assign its current category.
        /// 
        /// Note that this doesn't persist the new item to storage. Only returns
        /// it for the caller to work on some more.
        /// </remarks>
        /// <param name="txid">Id of existing transaction</param>
        public Task<Payee> NewFromTransaction(int txid)
        {
            // TODO: SingleAsync()
            var transaction = _context.Transactions.Where(x => x.ID == txid).Single();
            var result = new Payee() { Category = transaction.Category, Name = transaction.Payee.Trim() };

            return Task.FromResult(result);
        }
    }
}
