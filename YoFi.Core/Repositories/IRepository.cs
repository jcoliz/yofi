using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Repositories.Wire;

namespace YoFi.Core.Repositories;

/// <summary>
/// Provides access to <typeparamref name="T"/> model items, along with 
/// domain-specific business logic applicable to all model items
/// </summary>
/// <remarks>
/// Business logic specific to certain items will be in a derived
/// interface
/// </remarks>
/// <typeparam name="T">Type of model item we contain</typeparam>
public interface IRepository<T> where T: class //, IModelItem<T>
{
    #region CRUD operations

    /// <summary>
    /// Primary over-the-wire query operation to retrieve items
    /// </summary>
    /// <param name="parms">Parameters describing the desired items</param>
    /// <returns>Results</returns>
    Task<IWireQueryResult<T>> GetByQueryAsync(IWireQueryParameters parms);

    /// <summary>
    /// Over-the-wire method to get page size
    /// </summary>
    /// <returns>Current page size being used in query results</returns>
    Task<int> GetPageSizeAsync();

    /// <summary>
    /// Over-the-wire method to set page size
    /// </summary>
    /// <param name="value">page size to be used in query results</param>
    Task SetPageSizeAsync(int value);

    /// <summary>
    /// All known items
    /// </summary>
    /// <remarks>
    /// WARNING this is deprecated. I am working on removing it from the public
    /// interface.
    /// </remarks>
    IQueryable<T> All { get; }

    /// <summary>
    /// Retrieve a single item by <paramref name="id"/>
    /// </summary>
    /// <remarks>
    /// Will throw an exception if not found
    /// 
    /// Also note that you can send in x={id} as a query parameter into GetByQueryAsync
    /// if you don't want item tracking.
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
    /// Add <paramref name="items"/> to the repository
    /// </summary>
    /// <param name="items">Items we wish to add</param>
    Task BulkInsertAsync(IList<T> items);

    /// <summary>
    /// Update <paramref name="item"/> with new details
    /// </summary>
    /// <remarks>
    /// <paramref name="item"/> should be an object already retrieved through one of the properties
    /// or methods of this class.
    /// </remarks>
    /// <param name="item">New details</param>
    Task UpdateAsync(T item);

    /// <summary>
    /// Update <paramref name="items"/> with new details
    /// </summary>
    /// <remarks>
    /// <paramref name="items"/> should be objects already retrieved through one of the properties
    /// or methods of this class.
    /// </remarks>
    /// <param name="items">Items we wish to update</param>
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
    /// <returns>Stream containing the spreadsheet file</returns>
    Stream AsSpreadsheet();

    #endregion
}
