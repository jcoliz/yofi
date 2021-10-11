using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.AspNet
{
    public class PageDivider<T>
    {
        public const int DefaultPageSize = 25;

        public int PageSize { get; set; } = DefaultPageSize;

        public async Task<IQueryable<T>> ItemsForPage(IQueryable<T> result, int? p, ViewDataDictionary ViewData)
        {
            var count = await result.CountAsync();

            if (p.HasValue)
                ViewData["Page"] = p;

            int page = p ?? 1;

            var offset = (page - 1) * PageSize;
            ViewData["PageFirstItem"] = offset + 1;
            ViewData["PageLastItem"] = Math.Min(count, offset + PageSize);
            ViewData["PageTotalItems"] = count;

            if (count > PageSize)
            {
                if (page > 1)
                    ViewData["PreviousPage"] = page - 1;
                else
                    if ((page + 1) * PageSize < count)
                        ViewData["NextNextPage"] = page + 2;

                if (page * PageSize < count)
                    ViewData["NextPage"] = page + 1;
                else
                    if (page > 2)
                        ViewData["PreviousPreviousPage"] = page - 2;

                if (page > 2)
                    ViewData["FirstPage"] = 1;

                if ((page + 1) * PageSize < count)
                    ViewData["LastPage"] = 1 + (count - 1) / PageSize;
            }

            if (count > PageSize)
                result = result.Skip(offset).Take(PageSize);

            return result;
        }
    }
}
