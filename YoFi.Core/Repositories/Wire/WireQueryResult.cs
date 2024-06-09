using System.Collections.Generic;

namespace YoFi.Core.Repositories.Wire;

public class WireQueryResult<T> : IWireQueryResult<T> where T : class
{
    public IWireQueryParameters Parameters { get; set; }

    public IEnumerable<T> Items { get; set; }

    public IWirePageInfo PageInfo { get; set; }
}
