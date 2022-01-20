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
        public int? PreviousPage => (Page > 1) ? (Page - 1) : (int?)null;
        public int? NextNextPage => (Page == 1 && Page + 1 < PageInfo.TotalPages) ? Page + 2 : (int?)null;
        public int? NextPage => (Page < PageInfo.TotalPages) ? Page + 1 : (int?)null;
        public int? PreviousPreviousPage => ! NextPage.HasValue && Page > 2 ? Page - 2 : (int?)null;
        public int? FirstPage => (Page > 2 && PageInfo.TotalPages > 3) ? 1 : (int?)null;
        public int? LastPage => (Page + 1 < PageInfo.TotalPages && PageInfo.TotalPages > 3) ? PageInfo.TotalPages : (int?)null;

        public IWireQueryParameters Parameters { get; private set; }

        public IWirePageInfo PageInfo { get; private set; }

        public PageDivider(IWireQueryResultBase qresult)
        {
            PageInfo = qresult.PageInfo;
            Parameters = qresult.Parameters;
        }
    }
}
