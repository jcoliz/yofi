using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core.Repositories.Wire
{
    public class WirePageInfo : IWirePageInfo
    {
        public WirePageInfo(int totalitems, int page, int pagesize)
        {
            TotalItems = totalitems;
            Page = page;
            PageSize = pagesize;
            TotalPages = (TotalItems - 1) / PageSize + 1;

            var offset = (Page - 1) * PageSize;
            FirstItem = offset + 1;
            var last = Math.Min(TotalItems, offset + PageSize);
            NumItems = 1 + last - FirstItem;
        }

        public int Page { get; private set; }

        public int PageSize { get; private set; }

        public int FirstItem { get; private set; }

        public int NumItems { get; private set; }

        public int TotalPages { get; private set; }

        public int TotalItems { get; private set; }
    }
}
