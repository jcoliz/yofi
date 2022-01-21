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
    public interface IDatabaseAdministration
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
        Task ClearTestDataAsync(string id);

        /// <summary>
        /// Remove all items of the given type
        /// </summary>
        /// <remarks>
        /// Used by functional tests to clean up after themselves
        /// </remarks>
        /// <param name="id">Which data to clear (budgettx,payee,tx)</param>
        Task ClearDatabaseAsync(string id);
    }

    public interface IDatabaseStatus
    {
        bool IsEmpty { get; }
        int NumTransactions { get; }
        int NumBudgetTxs { get; }
        int NumPayees { get; }
    }
}
