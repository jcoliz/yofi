using jcoliz.OfficeOpenXml.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Repositories;
using YoFi.Core.Models;

namespace YoFi.Tests.Helpers
{
    public interface IMockRepository<T>: IRepository<T> where T: class, IModelItem
    {
        public void AddItems(int numitems);

        public T MakeItem(int x);

        public IEnumerable<T> MakeItems(int numitems);

        public bool Ok { get; set; }
    }

    public class MockBudgetTxRepository : IMockRepository<BudgetTx>
    {
        public void AddItems(int numitems) => Items.AddRange(MakeItems(numitems));

        static readonly DateTime defaulttimestamp = new DateTime(2020, 1, 1);

        public BudgetTx MakeItem(int x) => new BudgetTx() { ID = x, Amount = x, Category = x.ToString(), Timestamp = defaulttimestamp };

        public IEnumerable<BudgetTx> MakeItems(int numitems) => Enumerable.Range(1, numitems).Select(MakeItem);

        public bool Ok { get; set; } = true;

        public List<BudgetTx> Items { get; } = new List<BudgetTx>();

        public IQueryable<BudgetTx> All => Items.AsQueryable();

        public IQueryable<BudgetTx> OrderedQuery => throw new System.NotImplementedException();

        public Task AddAsync(BudgetTx item)
        {
            if (!Ok)
                throw new Exception("Failed");

            Items.Add(item);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<BudgetTx> items)
        {
            if (!Ok)
                throw new Exception("Failed");

            Items.AddRange(items);
            return Task.CompletedTask;
        }

        public Stream AsSpreadsheet()
        {
            if (!Ok)
                throw new Exception("Failed");

            var items = All;

            var stream = new MemoryStream();
            using (var ssw = new SpreadsheetWriter())
            {
                ssw.Open(stream);
                ssw.Serialize(items);
            }

            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        public IQueryable<BudgetTx> ForQuery(string q) => string.IsNullOrEmpty(q) ? All : All.Where(x => x.Category.Contains(q));

        public Task<BudgetTx> GetByIdAsync(int? id) => Ok ? Task.FromResult(All.Single(x => x.ID == id.Value)) : throw new Exception("Failed");

        public Task RemoveAsync(BudgetTx item)
        {
            if (!Ok)
                throw new Exception("Failed");

            if (item == null)
                throw new ArgumentException("Expected non-null item");

            var index = Items.FindIndex(x => x.ID == item.ID);
            Items.RemoveAt(index);

            return Task.CompletedTask;
        }

        public Task<bool> TestExistsByIdAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public Task UpdateAsync(BudgetTx item)
        {
            if (!Ok)
                throw new Exception("Failed");

            if (item == null)
                throw new ArgumentException("Expected non-null item");

            var index = Items.FindIndex(x => x.ID == item.ID);
            Items[index] = item;

            return Task.CompletedTask;
        }

        public IQueryable<BudgetTx> InDefaultOrder(IQueryable<BudgetTx> original) => original;
    }
}
