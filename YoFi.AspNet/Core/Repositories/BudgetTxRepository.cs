using System.Linq;
using YoFi.Core.Models;

namespace YoFi.Core.Repositories
{
    /// <summary>
    /// Contains a set of Budget Line Item (budgettx) model items and logic needed to operate on them
    /// </summary>
    /// <remarks>
    /// BudgetTx items are pretty simple. No additional logic. Just need to implement here how to sort the, and how to query them
    /// </remarks>
    public class BudgetTxRepository: BaseRepository<BudgetTx>
    {
        public BudgetTxRepository(IDataContext context): base(context)
        {
        }

        public override IQueryable<BudgetTx> ForQuery(string q) => string.IsNullOrEmpty(q) ? OrderedQuery : OrderedQuery.Where(x => x.Category.Contains(q));
    }
}
