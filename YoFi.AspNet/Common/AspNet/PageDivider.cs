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
        public const int DefaultPageSize = 25;

        public int PageSize { get; set; } = DefaultPageSize;
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

        public void BuildFromWirePageInfo(IWirePageInfo info)
        {
            PageSize = info.PageSize;
            Page = info.Page;
            PageTotalItems = info.TotalItems;
            PageFirstItem = info.FirstItem;
            PageLastItem = info.FirstItem + info.NumItems - 1;

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

        public Task<IQueryable<T>> ItemsForPage<T>(IQueryable<T> result, int? p)
        {
            // TODO: QueryExec CountAsync()
            var count = result.Count();

            Page = p ?? 1;

            var offset = (Page - 1) * PageSize;
            PageFirstItem = offset + 1;
            PageLastItem = Math.Min(count, offset + PageSize);
            PageTotalItems = count;

            if (count > PageSize)
            {
                if (Page > 1)
                    PreviousPage = Page - 1;
                else
                    if ((Page + 1) * PageSize < count)
                        NextNextPage = Page + 2;

                if (Page * PageSize < count)
                    NextPage = Page + 1;
                else
                    if (Page > 2)
                        PreviousPreviousPage = Page - 2;

                if (Page > 2)
                    FirstPage = 1;

                if ((Page + 1) * PageSize < count)
                    LastPage = 1 + (count - 1) / PageSize;
            }

            if (count > PageSize)
                result = result.Skip(offset).Take(PageSize);

            return Task.FromResult(result);
        }

        public class DefaultViewParameters : IViewParameters
        {
            public string QueryParameter { get; set; }

            public string ViewParameter { get; set; }

            public string OrderParameter { get; set; }
        }
    }
}
