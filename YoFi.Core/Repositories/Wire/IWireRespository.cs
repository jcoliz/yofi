using System.Collections.Generic;

namespace YoFi.Core.Repositories.Wire;

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
    
    /// <summary>
    /// Return all items which otherwise match the other paramters
    /// </summary>
    /// <remarks>
    /// That is, do not paginate the results
    /// </remarks>
    public bool All { get; }
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
public interface IWireQueryResult<out T>: IWireQueryResultBase
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
