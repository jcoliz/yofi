using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Repositories.Wire;

namespace Common.AspNet
{
    /// <summary>
    /// Used as a view model for a pagination control
    /// </summary>
    /// <remarks>
    /// Contains everything we need to show our standard pagination control
    /// </remarks>
    public class PageDivider
    {
        public int PageSize { get; set; }
        public int Page { get; private set; }
        public int PageFirstItem { get; private set; }
        public int PageLastItem { get; private set; }
        public int PageTotalItems { get; private set; }
        public int? PreviousPage { get; private set; }
        public int? NextNextPage { get; private set; }
        public int? NextPage { get; private set; }
        public int? PreviousPreviousPage { get; private set; }
        public int? FirstPage { get; private set; }
        public int? LastPage { get; private set; }
        public IViewParameters ViewParameters { get; set; }

        public PageDivider(IWireQueryResultBase qresult)
        {
            var info = qresult.PageInfo;
            var parms = qresult.Parameters;
            PageSize = info.PageSize;
            Page = info.Page;
            PageTotalItems = info.TotalItems;
            PageFirstItem = info.FirstItem;
            PageLastItem = info.FirstItem + info.NumItems - 1;

            ViewParameters = new DefaultViewParameters() { OrderParameter = parms.Order, QueryParameter = parms.Query, ViewParameter = parms.View };

            if (info.TotalItems > PageSize)
            {
                if (Page > 1)
                    PreviousPage = Page - 1;
                else
                    if ((Page + 1) * PageSize < info.TotalItems)
                    NextNextPage = Page + 2;

                if (Page * PageSize < info.TotalItems)
                    NextPage = Page + 1;
                else
                    if (Page > 2)
                    PreviousPreviousPage = Page - 2;

                if (Page > 2)
                    FirstPage = 1;

                if ((Page + 1) * PageSize < info.TotalItems)
                    LastPage = 1 + (info.TotalItems - 1) / PageSize;
            }
        }

        public class DefaultViewParameters : IViewParameters
        {
            public string QueryParameter { get; set; }

            public string ViewParameter { get; set; }

            public string OrderParameter { get; set; }
        }
    }
}
