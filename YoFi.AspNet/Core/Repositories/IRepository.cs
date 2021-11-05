using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Importers;
using YoFi.Core.Models;

namespace YoFi.Core.Repositories
{
    /// <summary>
    /// Provides access to <typeparamref name="T"/> model items, along with 
    /// domain-specific business logic applicable to all model items
    /// </summary>
    /// <remarks>
    /// Business logic specific to certain items will be in a derived
    /// interface
    /// </remarks>
    /// <typeparam name="T">Type of model item we contain</typeparam>
    public interface IRepository<T> : IImporter<T> where T: class, IModelItem<T>
    {
        #region CRUD operations
        /// <summary>
        /// All known items
        /// </summary>
        IQueryable<T> All { get; }

        /// <summary>
        /// All known items in the default order for <typeparamref name="T"/> items
        /// </summary>
        IQueryable<T> OrderedQuery { get; }

        /// <summary>
        /// Subset of all known items reduced by the specified query parameter
        /// </summary>
        /// <param name="q">Query describing the desired subset</param>
        /// <returns>Requested items</returns>
        IQueryable<T> ForQuery(string q);

        /// <summary>
        /// Retrieve a single item by <paramref name="id"/>
        /// </summary>
        /// <remarks>
        /// Will throw an exception if not found
        /// </remarks>
        /// <param name="id">Identifier of desired item</param>
        /// <returns>Desired item</returns>
        Task<T> GetByIdAsync(int? id);

        /// <summary>
        /// Determine whether a single item exists with the given <paramref name="id"/>
        /// </summary>
        /// <param name="id">Identifier of desired item</param>
        /// <returns>True if desired item exists</returns>
        Task<bool> TestExistsByIdAsync(int id);

        /// <summary>
        /// Add <paramref name="item"/> to the repository
        /// </summary>
        /// <param name="item">Item we wish to add</param>
        Task AddAsync(T item);

        /// <summary>
        /// Add <paramref name="items"/> to the repository
        /// </summary>
        /// <param name="items">Items we wish to add</param>
        Task AddRangeAsync(IEnumerable<T> items);

        /// <summary>
        /// Update <paramref name="item"/> with new details
        /// </summary>
        /// <remarks>
        /// <paramref name="item"/> should be an object already retrieved through one of the properties
        /// or methods of this class.
        /// </remarks>
        /// <param name="item">New details</param>
        Task UpdateAsync(T item);

        Task UpdateRangeAsync(IEnumerable<T> items);

        /// <summary>
        /// Remove <paramref name="item"/> from the repository
        /// </summary>
        /// <param name="item">Item to remove</param>
        Task RemoveAsync(T item);

        /// <summary>
        /// Remove <paramref name="items"/> from the repository
        /// </summary>
        /// <param name="items">Items to remove</param>
        Task RemoveRangeAsync(IEnumerable<T> items);
        #endregion

        #region Spreadsheet import/export

        /// <summary>
        /// Export all items to a spreadsheet, in default order
        /// </summary>
        /// <returns></returns>
        Stream AsSpreadsheet();

        #endregion
    }
}
