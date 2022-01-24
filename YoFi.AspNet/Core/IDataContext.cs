using System;
using System.Collections.Generic;
using System.Linq;
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
        #region Entities
        /// <summary>
        /// Retrieve the Transactions (with no splits)
        /// </summary>
        IQueryable<Transaction> Transactions { get; }
        /// <summary>
        /// Retrieve the Transactions with Splits attached
        /// </summary>
        IQueryable<Transaction> TransactionsWithSplits { get; }
        /// <summary>
        /// Retrieve the splits (with no transactions attached)
        /// </summary>
        IQueryable<Split> Splits { get; }
        /// <summary>
        /// Retrieve the splits (with transactions attached)
        /// </summary>
        IQueryable<Split> SplitsWithTransactions { get; }
        /// <summary>
        /// Retrieve the payees
        /// </summary>
        IQueryable<Payee> Payees { get; }
        /// <summary>
        /// Retrieve the budget line items
        /// </summary>
        IQueryable<BudgetTx> BudgetTxs { get; }

        /// <summary>
        /// Retrieve all <typeparamref name="T"/> entities 
        /// </summary>
        /// <typeparam name="T">Which type of entitites to get</typeparam>
        /// <returns></returns>
        IQueryable<T> Get<T>();
        #endregion

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

        Task<int> CountAsync<T>(IQueryable<T> query) where T : class;

        Task<bool> AnyAsync<T>(IQueryable<T> query) where T : class;

        /// <summary>
        /// Clear all items from a particular table
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<int> ClearAsync<T>() where T : class;
    }
}
