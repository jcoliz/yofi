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


        void Add(object item);
        void AddRange(IEnumerable<object> items);
        void Update(object item);
        void UpdateRange(IEnumerable<object> items);
        void Remove(object item);
        void RemoveRange(IEnumerable<object> items);
        Task SaveChangesAsync();

        // Async Query methods

        Task ToListAsync<T>(IQueryable<T> query);
    }
}
