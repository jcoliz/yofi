using System;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Models;
using YoFi.Core;

namespace YoFi.Core.Repositories
{
    public class PayeeRepository : BaseRepository<Payee>, IRepository<Payee>
    {
        public override IQueryable<Payee> InDefaultOrder(IQueryable<Payee> original) => original.OrderBy(x => x.Category).ThenBy(x => x.Name);

        public PayeeRepository(IDataContext context) : base(context)
        {
        }

        public IQueryable<Payee> ForQuery(string q)
        {
            throw new NotImplementedException();
        }

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

        public Task<Payee> NewFromTransaction(int txid)
        {
            // TODO: Find a way to call SingleAsync here

            var transaction = _context.Transactions.Where(x => x.ID == txid).Single();
            var result = new Payee() { Category = transaction.Category, Name = transaction.Payee.Trim() };

            return Task.FromResult(result);
        }
    }
}
