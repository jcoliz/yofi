using System;
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
    interface IAdministrative
    {
        /// <summary>
        /// Discover information about the current state of the database
        /// </summary>
        /// <returns></returns>
        Task<IDatabaseStatus> GetDatabaseStatus();

        /// <summary>
        /// Remove items with the test marker on them
        /// </summary>
        /// <remarks>
        /// Used by functional tests to clean up after themselves
        /// </remarks>
        /// <param name="id">Which data to clear (budgettx,payee,trx,all)</param>
        /// <returns></returns>
        Task ClearTestDataAsync(int id);

        /// <summary>
        /// Remove all items of the given type
        /// </summary>
        /// <remarks>
        /// Used by functional tests to clean up after themselves
        /// </remarks>
        /// <param name="id">Which data to clear (budgettx,payee,tx)</param>
        Task ClearDatabaseAsync(int id);
    }

    interface IDatabaseStatus
    {
        bool IsEmpty { get; }
        int NumTransactions { get; }
        int NumBudgetTx { get; }
        int NumPayees { get; }
    }
}
