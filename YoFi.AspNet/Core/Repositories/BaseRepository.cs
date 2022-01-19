using jcoliz.OfficeOpenXml.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories.Wire;

namespace YoFi.Core.Repositories
{
    /// <summary>
    /// Contains a set of <typeparamref name="T"/> model objects, with foundational application logic needed to work with those options
    /// </summary>
    /// <remarks>
    /// This base repository class largely implements the IRepository(T) interface, with some items left abstract for the inherited class
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class BaseRepository<T> : IWireRespository<T>, IRepository<T> where T: class, IModelItem<T>, new()
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

        /// <summary>
        /// All known items
        /// </summary>
        public IQueryable<T> All => _context.Get<T>();

        /// <summary>
        /// All known items in the default order for <typeparamref name="T"/> items
        /// </summary>
        public IQueryable<T> OrderedQuery => new T().InDefaultOrder(All);

        /// <summary>
        /// Subset of all known items reduced by the specified query parameter
        /// </summary>
        /// <param name="q">Query describing the desired subset</param>
        /// <returns>Requested items</returns>
        public virtual IQueryable<T> ForQuery(string q) => All;

        /// Retrieve a single item by <paramref name="id"/>
        /// </summary>
        /// <remarks>
        /// Will throw an exception if not found
        /// </remarks>
        /// <param name="id">Identifier of desired item</param>
        /// <returns>Desired item</returns>
        public Task<T> GetByIdAsync(int? id) => Task.FromResult(_context.Get<T>().Single(x => x.ID == id.Value));
        // TODO: QueryExec SingleAsync()

        /// <summary>
        /// Determine whether a single item exists with the given <paramref name="id"/>
        /// </summary>
        /// <param name="id">Identifier of desired item</param>
        /// <returns>True if desired item exists</returns>
        public Task<bool> TestExistsByIdAsync(int id) => Task.FromResult(_context.Get<T>().Any(x => x.ID == id));
        // TODO: QueryExec AnyAsync()

        /// <summary>
        /// Add <paramref name="item"/> to the repository
        /// </summary>
        /// <param name="item">Item we wish to add</param>
        public async Task AddAsync(T item)
        {
            _context.Add(item);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Add <paramref name="items"/> to the repository
        /// </summary>
        /// <param name="items">Items we wish to add</param>
        public async Task AddRangeAsync(IEnumerable<T> items)
        {
            _context.AddRange(items);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Update <paramref name="item"/> with new details
        /// </summary>
        /// <remarks>
        /// <paramref name="item"/> should be an object already retrieved through one of the properties
        /// or methods of this class.
        /// </remarks>
        /// <param name="item">New details</param>
        public Task UpdateAsync(T item)
        {
            _context.Update(item);
            return _context.SaveChangesAsync();
        }

        /// <summary>
        /// Update <paramref name="items"/> with new details
        /// </summary>
        /// <remarks>
        /// <paramref name="items"/> should be objects already retrieved through one of the properties
        /// or methods of this class.
        /// </remarks>
        /// <param name="items">Items we wish to update</param>
        public Task UpdateRangeAsync(IEnumerable<T> items)
        {
            _context.UpdateRange(items);
            return _context.SaveChangesAsync();
        }

        /// <summary>
        /// Remove <paramref name="item"/> from the repository
        /// </summary>
        /// <param name="item">Item to remove</param>
        public Task RemoveAsync(T item)
        {
            _context.Remove(item);
            return _context.SaveChangesAsync();
        }

        /// <summary>
        /// Remove <paramref name="items"/> from the repository
        /// </summary>
        /// <param name="items">Items to remove</param>
        public Task RemoveRangeAsync(IEnumerable<T> items)
        {
            _context.RemoveRange(items);
            return _context.SaveChangesAsync();
        }
        #endregion

        #region Wire Interface
        public async Task<IWireQueryResult<T>> GetByQueryAsync(IWireQueryParameters parms)
        {
            var query = ForQuery(parms.Query);

            var count = await _context.CountAsync(query);
            var pages = new WirePageInfo(totalitems: count, page: parms.Page ?? 1, pagesize: PageSize);
            if (count > PageSize)
                query = query.Skip(pages.FirstItem - 1).Take(pages.NumItems);

            var list = await _context.ToListNoTrackingAsync(query);
            IWireQueryResult<T> result = new WireQueryResult<T>() { Items = list, PageInfo = pages, Parameters = parms };
            return result;
        }

        public const int DefaultPageSize = 25;

        private int PageSize = DefaultPageSize;

        public Task<int> GetPageSizeAsync() => Task.FromResult(PageSize);

        public Task SetPageSizeAsync(int value)
        {
            PageSize = value;

            return Task.CompletedTask;
        }

        #endregion

        #region Exporter

        /// <summary>
        /// Export all items to a spreadsheet, in default order
        /// </summary>
        /// <returns>Stream containing the spreadsheet file</returns>

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
