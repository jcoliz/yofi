using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core.Repositories.Wire
{
    public class WireQueryResult<T> : IWireQueryResult<T> where T : class
    {
        public IWireQueryParameters Parameters { get; set; }

        public IEnumerable<T> Items { get; set; }

        public IWirePageInfo PageInfo { get; set; }
    }
}
