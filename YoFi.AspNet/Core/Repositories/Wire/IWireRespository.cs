using System;
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
    public interface IWireRespository<T> where T: class
    {
        Task<IWireQueryResult<T>> GetByQueryAsync(IWireQueryParameters parms);

        Task<int> GetPageSizeAsync();

        Task SetPageSizeAsync(int value);
    }

    public interface IWireQueryParameters
    {
        string Query { get; }
        int? Page { get; }
        string Order { get; }
    }

    public class WireQueryParameters : IWireQueryParameters
    {
        public string Query { get; set; }

        public int? Page { get; set; }

        public string Order { get; set; }
    }

    public interface IWireQueryResult<T>
    {
        /// <summary>
        /// Parameters used to generate this query
        /// </summary>
        IWireQueryParameters Parameters { get; }

        IEnumerable<T> Items { get; }

        IWirePageInfo PageInfo { get; }
    }

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
