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
        public bool ShowPreviousPage => (PageInfo.Page > 1);
        public bool ShowNextNextPage => (PageInfo.Page == 1 && PageInfo.Page + 1 < PageInfo.TotalPages);
        public bool ShowNextPage => (PageInfo.Page < PageInfo.TotalPages);
        public bool ShowPreviousPreviousPage => !ShowNextPage && PageInfo.Page > 2;
        public bool ShowFirstPage => (PageInfo.Page > 2 && PageInfo.TotalPages > 3);
        public bool ShowLastPage => (PageInfo.Page + 1 < PageInfo.TotalPages && PageInfo.TotalPages > 3);

        public IWireQueryParameters Parameters { get; private set; }

        public IWirePageInfo PageInfo { get; private set; }

        public PageDivider(IWireQueryResultBase qresult)
        {
            PageInfo = qresult.PageInfo;
            Parameters = qresult.Parameters;
        }
    }
}
