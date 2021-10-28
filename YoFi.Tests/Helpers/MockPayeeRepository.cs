using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.Tests.Helpers
{
    public class MockPayeeRepository : BaseMockRepository<Payee>, IPayeeRepository
    {
        public override Payee MakeItem(int x) => new Payee() { ID = x, Category = x.ToString(), Name = x.ToString() };
        public override IQueryable<Payee> ForQuery(string q) => string.IsNullOrEmpty(q) ? All : All.Where(x => x.Category.Contains(q) || x.Name.Contains(q));

        public Task BulkEdit(string category)
        {
            throw new NotImplementedException();
        }

        public Task<Payee> NewFromTransaction(int txid)
        {
            throw new NotImplementedException();
        }
    }
}
