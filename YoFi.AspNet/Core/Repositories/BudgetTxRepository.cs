using System.Linq;
using YoFi.Core.Models;

namespace YoFi.Core.Repositories
{
    /// <summary>
    /// Contains a set of Budget Line Item (budgettx) model items and logic needed to operate on them
    /// </summary>
    /// <remarks>
    /// BudgetTx items are pretty simple. No additional logic. Just need to implement here how to query them
    /// </remarks>
    public class BudgetTxRepository: BaseRepository<BudgetTx>
    {
        public BudgetTxRepository(IDataContext context): base(context)
        {
        }

        /// <summary>
        /// Subset of all known items reduced by the specified query parameter
        /// </summary>
        /// <param name="q">Query describing the desired subset</param>
        /// <returns>Requested items</returns>
        public override IQueryable<BudgetTx> ForQuery(string q) => string.IsNullOrEmpty(q) ? OrderedQuery : OrderedQuery.Where(x => x.Category.Contains(q));
    }
}
