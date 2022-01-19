using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core.Repositories.Wire
{
    /// <summary>
    /// Repository accessed "over the wire", not on the same machine as the UI
    /// </summary>
    /// <remarks>
    /// Repositories implementing this can be accesswed without sharing memory space or compute
    /// with the calling UI. 
    /// 
    /// A repository implementing this will have to return complete objects upward, not queryables,
    /// and ergo must execute its own queries, not let the UI do it.
    /// </remarks>
    public interface IWireRespository<T>: IRepository<T> where T: class
    {
        Task<IWireQueryResult<T>> GetByQueryAsync(IWireQueryParameters parms);

        Task<int> GetPageSizeAsync();

        Task SetPageSizeAsync(int value);
    }

    /// <summary>
    /// The parameters which can be used to query results from a wire repository 
    /// </summary>
    /// <remarks>
    /// The exact meaning of each item is up to the repository itself
    /// </remarks>
    public interface IWireQueryParameters
    {
        string Query { get; }
        int? Page { get; }
        string Order { get; }
        public string View { get; }
    }

    /// <summary>
    /// The common base which all wire query results will follow
    /// </summary>
    /// <remarks>
    /// Useful for cases where type of items don't matter
    /// </remarks>
    public interface IWireQueryResultBase
    {
        /// <summary>
        /// Parameters used to generate this query
        /// </summary>
        IWireQueryParameters Parameters { get; }

        IWirePageInfo PageInfo { get; }
    }

    /// <summary>
    /// The result of a wire repository query
    /// </summary>
    /// <typeparam name="T">Type of items returned</typeparam>
    public interface IWireQueryResult<T>: IWireQueryResultBase
    {
        /// <summary>
        /// Items which were found as a result of the query
        /// </summary>
        IEnumerable<T> Items { get; }
    }

    /// <summary>
    /// Information about the page of results which was returned
    /// </summary>
    public interface IWirePageInfo
    {
        /// <summary>
        /// Which page of results are these?
        /// </summary>
        int Page { get; }

        /// <summary>
        /// The standard page size used to divide pages
        /// </summary>
        int PageSize { get; }

        /// <summary>
        /// Which item# is the first one on this page
        /// </summary>
        int FirstItem { get; }

        /// <summary>
        /// How many items were returned on this page
        /// </summary>
        int NumItems { get; }

        /// <summary>
        /// The highest valid page# for this dataset
        /// </summary>
        int TotalPages { get; }

        /// <summary>
        /// How many items could possibly be returned
        /// </summary>
        int TotalItems { get; }
    }
}
