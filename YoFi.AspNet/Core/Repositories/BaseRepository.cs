using jcoliz.OfficeOpenXml.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core;

namespace YoFi.Core.Repositories
{
    /// <summary>
    /// Contains a set of <typeparamref name="T"/> model objects, with foundational application logic needed to work with those options
    /// </summary>
    /// <remarks>
    /// This base repository class largely implements the IRepository(T) interface, with some items left abstract for the inherited class
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseRepository<T> : IRepository<T> where T: class, IModelItem
    {
        protected readonly IDataContext _context;

        public IQueryable<T> All => _context.Get<T>();

        public IQueryable<T> OrderedQuery => InDefaultOrder(All);

        public BaseRepository(IDataContext context)
        {
            _context = context;
        }

        public abstract IQueryable<T> InDefaultOrder(IQueryable<T> original);

        // TODO: SingleAsync()
        public Task<T> GetByIdAsync(int? id) => Task.FromResult(_context.Get<T>().Single(x => x.ID == id.Value));

        // TODO: AnyAsync()
        public Task<bool> TestExistsByIdAsync(int id) => Task.FromResult(_context.Get<T>().Any(x => x.ID == id));

        public async Task AddAsync(T item)
        {
            await _context.AddAsync(item);
            await _context.SaveChangesAsync();
        }
        public async Task AddRangeAsync(IEnumerable<T> items)
        {
            await _context.AddRangeAsync(items);
            await _context.SaveChangesAsync();
        }

        public Task UpdateAsync(T item)
        {
            _context.Update(item);
            return _context.SaveChangesAsync();
        }

        public Task RemoveAsync(T item)
        {
            _context.Remove(item);
            return _context.SaveChangesAsync();
        }

        public Stream AsSpreadsheet()
        {
            var stream = new MemoryStream();
            using (var ssw = new SpreadsheetWriter())
            {
                ssw.Open(stream);
                ssw.Serialize(OrderedQuery);
            }

            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        public abstract IQueryable<T> ForQuery(string q);
    }
}
