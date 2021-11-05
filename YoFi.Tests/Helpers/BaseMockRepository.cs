using jcoliz.OfficeOpenXml.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.Tests.Helpers
{
    public abstract class BaseMockRepository<T>: IMockRepository<T> where T: class, IModelItem<T>, new()
    {
        private readonly HashSet<T> _importing;

        protected BaseMockRepository()
        {
            _importing = new HashSet<T>(new T().ImportDuplicateComparer);
        }

        public void AddItems(int numitems) => Items.AddRange(MakeItems(numitems));

        public abstract T MakeItem(int x);

        public IEnumerable<T> MakeItems(int numitems) => Enumerable.Range(1, numitems).Select(MakeItem);

        public List<T> Items { get; } = new List<T>();

        public IQueryable<T> All => Items.AsQueryable();

        public IQueryable<T> OrderedQuery => throw new System.NotImplementedException();

        public Task AddAsync(T item)
        {
            Items.Add(item);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<T> items)
        {
            Items.AddRange(items);
            return Task.CompletedTask;
        }

        public Stream AsSpreadsheet()
        {
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

        public Task<T> GetByIdAsync(int? id) => Task.FromResult(All.Single(x => x.ID == id.Value));

        public Task RemoveAsync(T item)
        {
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

        public Task UpdateAsync(T item)
        {
            if (item == null)
                throw new ArgumentException("Expected non-null item");

            var index = Items.FindIndex(x => x.ID == item.ID);
            Items[index] = item;

            return Task.CompletedTask;
        }

        public abstract IQueryable<T> ForQuery(string q);

        public void QueueImportFromXlsx(Stream stream)
        {
            using var ssr = new SpreadsheetReader();
            ssr.Open(stream);
            var items = ssr.Deserialize<T>(exceptproperties: new string[] { "ID" });
            _importing.UnionWith(items);
        }

        public async Task<IEnumerable<T>> ProcessImportAsync()
        {
            // Remove duplicate items
            var result = _importing.Except(All).ToList();

            // Add remaining items
            await AddRangeAsync(result);

            // Clear import queue for next time
            _importing.Clear();

            // Return those items for display
            return new T().InDefaultOrder(result.AsQueryable());
        }

        public Task RemoveRangeAsync(IEnumerable<T> items)
        {
            throw new NotImplementedException();
        }

        public Task UpdateRangeAsync(IEnumerable<T> items)
        {
            throw new NotImplementedException();
        }
    }
}
