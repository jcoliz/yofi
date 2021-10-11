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

            if (!p.HasValue)
                p = 1;
            else
                ViewData["Page"] = p;

            var offset = (p.Value - 1) * PageSize;
            ViewData["PageFirstItem"] = offset + 1;
            ViewData["PageLastItem"] = Math.Min(count, offset + PageSize);
            ViewData["PageTotalItems"] = count;

            if (count > PageSize)
            {
                if (p > 1)
                    ViewData["PreviousPage"] = p.Value - 1;
                else
                    if ((p + 1) * PageSize < count)
                    ViewData["NextNextPage"] = p.Value + 2;

                if (p * PageSize < count)
                    ViewData["NextPage"] = p.Value + 1;
                else
                    if (p > 2)
                    ViewData["PreviousPreviousPage"] = p.Value - 2;

                if (p > 2)
                    ViewData["FirstPage"] = 1;

                if ((p + 1) * PageSize < count)
                    ViewData["LastPage"] = 1 + (count - 1) / PageSize;
            }

            if (count > PageSize)
                result = result.Skip(offset).Take(PageSize);

            return result;
        }
    }
}
