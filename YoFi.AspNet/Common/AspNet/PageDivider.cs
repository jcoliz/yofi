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
    /// Contains everything we need to show our standard pagination control. Think of this as the
    /// "code behind" for the pagination partial view
    /// </remarks>
    public class PageDivider: IWireQueryResultBase
    {
        public int PageSize => PageInfo.PageSize;
        public int Page => PageInfo.Page;
        public int PageFirstItem => PageInfo.FirstItem;
        public int PageLastItem => PageInfo.FirstItem + PageInfo.NumItems - 1;
        public int PageTotalItems => PageInfo.TotalItems;

        public bool ShowPreviousPage => (Page > 1);
        public bool ShowNextNextPage => (Page == 1 && Page + 1 < PageInfo.TotalPages);
        public bool ShowNextPage => (Page < PageInfo.TotalPages);
        public bool ShowPreviousPreviousPage => !ShowNextPage && Page > 2;
        public bool ShowFirstPage => (Page > 2 && PageInfo.TotalPages > 3);
        public bool ShowLastPage => (Page + 1 < PageInfo.TotalPages && PageInfo.TotalPages > 3);

        public IWireQueryParameters Parameters { get; private set; }

        public IWirePageInfo PageInfo { get; private set; }

        public PageDivider(IWireQueryResultBase qresult)
        {
            PageInfo = qresult.PageInfo;
            Parameters = qresult.Parameters;
        }
    }
}
