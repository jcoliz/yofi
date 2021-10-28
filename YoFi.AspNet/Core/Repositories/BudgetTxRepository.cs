using System.Linq;
using YoFi.Core.Models;

namespace YoFi.Core.Repositories
{
    public class BudgetTxRepository: BaseRepository<BudgetTx>, IRepository<BudgetTx>
    {
        public override IQueryable<BudgetTx> InDefaultOrder(IQueryable<BudgetTx> original) => original.OrderByDescending(x => x.Timestamp.Year).ThenByDescending(x => x.Timestamp.Month).ThenBy(x => x.Category);

        public BudgetTxRepository(IDataContext context): base(context)
        {
        }

        public IQueryable<BudgetTx> ForQuery(string q) => string.IsNullOrEmpty(q) ? OrderedQuery : OrderedQuery.Where(x => x.Category.Contains(q));
    }
}
