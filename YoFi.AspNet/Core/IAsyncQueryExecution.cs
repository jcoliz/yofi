using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core
{
    /// <summary>
    /// A facility for executing queries asyncrhonously
    /// </summary>
    /// <remarks>
    /// EF Core enables async queries. We want to be running those in production.
    /// However, only EF Core can generate queries which can be run asyncronously.
    /// So if we want to call e.g. ToListAsync() we have to take a depenency on EF Core,
    /// which we really dont want.
    /// </remarks>
    public interface IAsyncQueryExecution
    {
        /// <summary>
        /// Execute ToList query asynchronously, with no tracking
        /// </summary>
        /// <typeparam name="T">Type of entities being queried</typeparam>
        /// <param name="query">Query to execute</param>
        /// <returns>List of items</returns>
        Task<List<T>> ToListNoTrackingAsync<T>(IQueryable<T> query) where T : class;
    }
}
