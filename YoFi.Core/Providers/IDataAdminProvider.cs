﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core
{
    /// <summary>
    /// UI-facing administrative capabilities
    /// </summary>
    /// <remarks>
    /// Note that this is a wire interface, so no properties, everything is async
    /// </remarks>
    public interface IDataAdminProvider
    {
        /// <summary>
        /// Discover information about the current state of the database
        /// </summary>
        /// <returns></returns>
        Task<IDataStatus> GetDatabaseStatus();

        /// <summary>
        /// Remove items with the test marker on them
        /// </summary>
        /// <remarks>
        /// Used by functional tests to clean up after themselves
        /// </remarks>
        /// <param name="id">Which data to clear (budgettx,payee,trx,all)</param>
        /// <returns></returns>
        Task ClearTestDataAsync(string id);

        /// <summary>
        /// Remove all items of the given type
        /// </summary>
        /// <remarks>
        /// Used by functional tests to clean up after themselves
        /// </remarks>
        /// <param name="id">Which data to clear (budgettx,payee,tx)</param>
        Task ClearDatabaseAsync(string id);

        /// <summary>
        /// Mark all hidden transactions on or before today as unhidden
        /// </summary>
        /// <remarks>
        /// Useful for demo if you want to reveal them over time for a demo
        /// </remarks>
        /// <returns></returns>
        Task UnhideTransactionsToToday();
    }

    public interface IDataStatus
    {
        bool IsEmpty { get; }
        int NumTransactions { get; }
        int NumBudgetTxs { get; }
        int NumPayees { get; }
    }
}
