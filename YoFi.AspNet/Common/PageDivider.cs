using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.AspNet
{
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

        public Task<IQueryable<T>> ItemsForPage<T>(IQueryable<T> result, int? p)
        {
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
    }
}
