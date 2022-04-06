using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.Core
{
    /// <summary>
    /// Data storage context
    /// </summary>
    /// <remarks>
    /// Platform and storage agnostic interface for all data stored in the system
    /// </remarks>
    public interface IDataContext
    {
        /// <summary>
        /// Get a query of <typeparamref name="T"/> entities 
        /// </summary>
        /// <typeparam name="T">Which type of entitites to get</typeparam>
        IQueryable<T> Get<T>() where T : class;

        /// <summary>
        /// Get a query of <typeparamref name="TEntity"/> entities including 
        /// an expression of <typeparamref name="Property"/> properties
        /// </summary>
        /// <typeparam name="TEntity">Which type of entitites to get</typeparam>
        /// <typeparam name="TProperty">Type of properties</typeparam>
        /// <param name="navigationPropertyPath">Navigation expression to describe properties</param>
        IQueryable<TEntity> GetIncluding<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath) where TEntity : class;

        /// <summary>
        /// Add an item
        /// </summary>
        /// <param name="item">Item to add</param>
        void Add(object item);

        /// <summary>
        /// Add a range of items
        /// </summary>
        /// <param name="items">Items to add</param>
        void AddRange(IEnumerable<object> items);
       
        /// <summary>
        /// Update an item
        /// </summary>
        /// <param name="item">Item to update</param>
        void Update(object item);
        
        /// <summary>
        /// Update a range of items
        /// </summary>
        /// <param name="items">Items to update</param>
        void UpdateRange(IEnumerable<object> items);
        
        /// <summary>
        /// Remove an item
        /// </summary>
        /// <param name="item">Item to remove</param>
        void Remove(object item);
        
        /// <summary>
        /// Remove items
        /// </summary>
        /// <param name="items">Items to remove</param>
        void RemoveRange(IEnumerable<object> items);
        
        /// <summary>
        /// Save changes previously made
        /// </summary>
        /// <remarks>
        /// This is only needed in the case where we made changes to tracked objects and
        /// did NOT call update on them. Should be rare.
        /// </remarks>
        Task SaveChangesAsync();

        /// <summary>
        /// Execute ToList query asynchronously, with no tracking
        /// </summary>
        /// <typeparam name="T">Type of entities being queried</typeparam>
        /// <param name="query">Query to execute</param>
        /// <returns>List of items</returns>
        Task<List<T>> ToListNoTrackingAsync<T>(IQueryable<T> query) where T : class;

        /// <summary>
        /// Execute Count query asynchronously
        /// </summary>
        /// <typeparam name="T">Type of entities being queried</typeparam>
        /// <param name="query">Query to execute</param>
        /// <returns>Number of items in <paramref name="query"/></returns>
        Task<int> CountAsync<T>(IQueryable<T> query) where T : class;

        /// <summary>
        /// Execute "Any" query asynchronously
        /// </summary>
        /// <typeparam name="T">Type of entities being queried</typeparam>
        /// <param name="query">Query to execute</param>
        /// <returns>Whether there are any items in <paramref name="query"/></returns>
        Task<bool> AnyAsync<T>(IQueryable<T> query) where T : class;

        /// <summary>
        /// Clear all items from a particular table
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<int> ClearAsync<T>() where T : class;

        /// <summary>
        /// Insert many items en masse
        /// </summary>
        /// <remarks>
        /// This is much more efficient than doing it one at a time
        /// </remarks>
        /// <typeparam name="T">Type of items</typeparam>
        /// <param name="items">Items to be inserted</param>
        Task BulkInsertAsync<T>(IList<T> items) where T : class;

        /// <summary>
        /// Delete many items en masse
        /// </summary>
        /// <remarks>
        /// This is much more efficient than doing it one at a time
        /// </remarks>
        /// <typeparam name="T">Type of items</typeparam>
        /// <param name="items">Items to be deleted</param>
        Task BulkDeleteAsync<T>(IQueryable<T> items) where T : class;

        /// <summary>
        /// Update many items en masse
        /// </summary>
        /// <remarks>
        /// This is much more efficient than doing it one at a time
        /// </remarks>
        /// <typeparam name="T">Type of items</typeparam>
        /// <param name="items">Items to be updated</param>
        /// <param name="newvalues">Type T object with affected fields set to new values</param>
        /// <param name="columns">List of affected fields</param>
        /// <returns></returns>
        Task BulkUpdateAsync<T>(IQueryable<T> items, T newvalues, List<string> columns) where T : class;
    }
}
