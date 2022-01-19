using jcoliz.OfficeOpenXml.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Repositories;
using YoFi.Core.Models;
using YoFi.Core.Repositories.Wire;

namespace YoFi.Tests.Helpers
{
    public interface IMockRepository<T>: IRepository<T> where T: class, IModelItem<T>
    {
        public void AddItems(int numitems);

        public T MakeItem(int x);

        public IEnumerable<T> MakeItems(int numitems);
    }

    public class MockBudgetTxRepository : BaseMockRepository<BudgetTx>, IBudgetTxRepository
    {
        static readonly DateTime defaulttimestamp = new DateTime(2020, 1, 1);

        public override BudgetTx MakeItem(int x) => new BudgetTx() { ID = x, Amount = x, Category = x.ToString(), Timestamp = defaulttimestamp };

        public override IQueryable<BudgetTx> ForQuery(string q) => string.IsNullOrEmpty(q) ? All : All.Where(x => x.Category.Contains(q));
        public bool WasBulkDeleteCalled { get; private set; } = false;

        public Task BulkDeleteAsync()
        {
            // We don't need to DO anything here.
            WasBulkDeleteCalled = true;
            return Task.CompletedTask;
        }

        public Task<IWireQueryResult<BudgetTx>> GetByQueryAsync(IWireQueryParameters parms)
        {
            // So, I just copied the code from the production BaseRepository

            var query = ForQuery(parms.Query);

            var count = query.Count();
            const int pagesize = 25;
            var pages = new WirePageInfo(totalitems: count, page: parms.Page ?? 1, pagesize: pagesize);

            if (count > pagesize)
                query = query.Skip(pages.FirstItem - 1).Take(pages.NumItems);

            var list = query.ToList();
            IWireQueryResult<BudgetTx> result = new WireQueryResult<BudgetTx>() { Items = list, PageInfo = pages, Parameters = parms };
            return Task.FromResult(result);
        }

        public Task<int> GetPageSizeAsync()
        {
            throw new NotImplementedException();
        }

        public Task SetPageSizeAsync(int value)
        {
            throw new NotImplementedException();
        }
    }
}
