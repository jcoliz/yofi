using jcoliz.OfficeOpenXml.Serializer;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Data;
using YoFi.AspNet.Models;
using YoFi.Core;

namespace YoFi.AspNet.Core.Repositories
{
    public class BudgetTxRepository: BaseRepository<BudgetTx>
    {

        public IQueryable<BudgetTx> OrderedQuery => All.OrderByDescending(x => x.Timestamp.Year).ThenByDescending(x => x.Timestamp.Month).ThenBy(x => x.Category).AsQueryable();

        public BudgetTxRepository(ApplicationDbContext context): base(context)
        {

        }

        public BudgetTxRepository(IDataContext context): base(context)
        {
        }

        public IQueryable<BudgetTx> ForQuery(string q) => string.IsNullOrEmpty(q) ? OrderedQuery : OrderedQuery.Where(x => x.Category.Contains(q));
    }
}
