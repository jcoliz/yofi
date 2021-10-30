using System;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core;

namespace YoFi.Core.Repositories
{
    /// <summary>
    /// Contains a set of Payee model items and logic needed to operate on them
    /// </summary>
    public class PayeeRepository : BaseRepository<Payee>, IPayeeRepository
    {
        public PayeeRepository(IDataContext context) : base(context)
        {
        }

        public override IQueryable<Payee> ForQuery(string q) => string.IsNullOrEmpty(q) ? OrderedQuery : OrderedQuery.Where(x => x.Category.Contains(q) || x.Name.Contains(q));

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

        public async Task BulkDelete()
        {
            _context.RemoveRange(All.Where(x => x.Selected == true));
            await _context.SaveChangesAsync();
        }

        public Task<Payee> NewFromTransaction(int txid)
        {
            // TODO: SingleAsync()
            var transaction = _context.Transactions.Where(x => x.ID == txid).Single();
            var result = new Payee() { Category = transaction.Category, Name = transaction.Payee.Trim() };

            return Task.FromResult(result);
        }
    }
}
