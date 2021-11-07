using jcoliz.OfficeOpenXml.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.Core.Repositories
{
    /// <summary>
    /// Contains a set of <typeparamref name="T"/> model objects, with foundational application logic needed to work with those options
    /// </summary>
    /// <remarks>
    /// This base repository class largely implements the IRepository(T) interface, with some items left abstract for the inherited class
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseRepository<T> : IRepository<T> where T: class, IModelItem<T>, new()
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks>
        /// Typically filled in by the frameworks' dependency injection
        /// </remarks>
        /// <param name="context">Data conext whe4re our data is to be found</param>
        public BaseRepository(IDataContext context)
        {
            _context = context;
        }
        #endregion

        #region CRUD Operations
        public IQueryable<T> All => _context.Get<T>();

        public IQueryable<T> OrderedQuery => new T().InDefaultOrder(All);

        // TODO: SingleAsync()
        public Task<T> GetByIdAsync(int? id) => Task.FromResult(_context.Get<T>().Single(x => x.ID == id.Value));

        // TODO: AnyAsync()
        public Task<bool> TestExistsByIdAsync(int id) => Task.FromResult(_context.Get<T>().Any(x => x.ID == id));

        public abstract IQueryable<T> ForQuery(string q);

        public async Task AddAsync(T item)
        {
            _context.Add(item);
            await _context.SaveChangesAsync();
        }
        public async Task AddRangeAsync(IEnumerable<T> items)
        {
            _context.AddRange(items);
            await _context.SaveChangesAsync();
        }

        public Task UpdateAsync(T item)
        {
            _context.Update(item);
            return _context.SaveChangesAsync();
        }
        public Task UpdateRangeAsync(IEnumerable<T> items)
        {
            _context.UpdateRange(items);
            return _context.SaveChangesAsync();
        }

        public Task RemoveAsync(T item)
        {
            _context.Remove(item);
            return _context.SaveChangesAsync();
        }
        public Task RemoveRangeAsync(IEnumerable<T> items)
        {
            _context.RemoveRange(items);
            return _context.SaveChangesAsync();
        }
        #endregion

        #region Exporter
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
        #endregion

        #region Fields
        /// <summary>
        /// Data context where our data is to be found
        /// </summary>
        protected readonly IDataContext _context;

        #endregion

    }
}
