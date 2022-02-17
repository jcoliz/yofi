using jcoliz.OfficeOpenXml.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories.Wire;

namespace YoFi.Tests.Helpers
{
    public abstract class BaseMockRepository<T>: IMockRepository<T> where T: class, IModelItem<T>, new()
    {
        protected BaseMockRepository()
        {
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

            if (Items.Any() && Items.Any(x=>x.ID == default))
            {
                var nextid = Items.Max(x => x.ID) + 1;
                foreach (var item in Items)
                    if (item.ID == default)
                        item.ID = nextid++;
            }

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


        public Task RemoveRangeAsync(IEnumerable<T> items)
        {
            throw new NotImplementedException();
        }

        public Task UpdateRangeAsync(IEnumerable<T> items)
        {
            throw new NotImplementedException();
        }

        public Task<IWireQueryResult<T>> GetByQueryAsync(IWireQueryParameters parms)
        {
            // So, I just copied the code from the production BaseRepository

            var query = ForQuery(parms.Query);

            var count = query.Count();
            const int pagesize = 25;
            var pages = new WirePageInfo(totalitems: count, page: parms.Page ?? 1, pagesize: pagesize);

            if (count > pagesize)
                query = query.Skip(pages.FirstItem - 1).Take(pages.NumItems);

            var list = query.ToList();
            IWireQueryResult<T> result = new WireQueryResult<T>() { Items = list, PageInfo = pages, Parameters = parms };
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

        public Task BulkInsertAsync(IList<T> items) => AddRangeAsync(items);
    }
}
