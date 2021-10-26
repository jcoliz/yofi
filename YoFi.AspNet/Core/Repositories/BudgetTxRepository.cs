using jcoliz.OfficeOpenXml.Serializer;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Models;
using YoFi.Core;

namespace YoFi.AspNet.Core.Repositories
{
    public class BudgetTxRepository
    {
        private readonly IDataContext _context;

        public IQueryable<BudgetTx> OrderedQuery => _context.BudgetTxs.OrderByDescending(x => x.Timestamp.Year).ThenByDescending(x => x.Timestamp.Month).ThenBy(x => x.Category).AsQueryable();

        public IQueryable<BudgetTx> All => _context.BudgetTxs;

        public BudgetTxRepository(IDataContext context)
        {
            _context = context;
        }

        public IQueryable<BudgetTx> ForQuery(string q) => string.IsNullOrEmpty(q) ? OrderedQuery : OrderedQuery.Where(x => x.Category.Contains(q));

        // TODO: I would like to figure out how to let EF return a SingleAsync
        public Task<BudgetTx> GetByIdAsync(int? id) => Task.FromResult(_context.BudgetTxs.Single(x => x.ID == id.Value));

        public Task<bool> TestExistsByIdAsync(int id) => Task.FromResult(_context.BudgetTxs.Any(x => x.ID == id));

        public async Task AddAsync(BudgetTx item)
        {
            await _context.AddAsync(item);
            await _context.SaveChangesAsync();
        }
        public async Task AddRangeAsync(IEnumerable<BudgetTx> items)
        {
            await _context.AddRangeAsync(items);
            await _context.SaveChangesAsync();
        }

        public Task UpdateAsync(BudgetTx item)
        {
            _context.Update(item);
            return _context.SaveChangesAsync();
        }

        public Task RemoveAsync(BudgetTx item)
        {
            _context.Remove(item);
            return _context.SaveChangesAsync();
        }

        public Stream AsSpreadsheet()
        {
            var items = OrderedQuery;

            var stream = new MemoryStream();
            using (var ssw = new SpreadsheetWriter())
            {
                ssw.Open(stream);
                ssw.Serialize(items);
            }

            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }
    }
}
