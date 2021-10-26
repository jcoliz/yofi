using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Data;
using YoFi.AspNet.Models;

namespace YoFi.AspNet.Core.Repositories
{
    public class BudgetTxRepository
    {
        private readonly ApplicationDbContext _context;

        public IQueryable<BudgetTx> OrderedQuery => _context.BudgetTxs.OrderByDescending(x => x.Timestamp.Year).ThenByDescending(x => x.Timestamp.Month).ThenBy(x => x.Category).AsQueryable();

        public IQueryable<BudgetTx> All => _context.BudgetTxs;

        public BudgetTxRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<BudgetTx> GetByIdAsync(int? id) => _context.BudgetTxs.SingleAsync(x => x.ID == id.Value);

        public Task<bool> TestExistsByIdAsync(int id) => _context.BudgetTxs.AnyAsync(x => x.ID == id);

        public Task AddAsync(BudgetTx item)
        {
            _context.Add(item);
            return _context.SaveChangesAsync();
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
            _context.BudgetTxs.Remove(item);
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
