using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.AspNet.Data;
using YoFi.Core;

namespace YoFi.Tests.Helpers
{
    /// <summary>
    /// IN-memory version of application context
    /// </summary>
    /// <remarks>
    /// There are a few things we can't do in an inmemory DB, so we work around those here.
    /// </remarks>
    public class ApplicationDbContextInMemory : ApplicationDbContext, IDataContext
    {
        public ApplicationDbContextInMemory(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        async Task IDataContext.BulkInsertAsync<T>(IList<T> items)
        {
            await this.AddRangeAsync(items);
            await this.SaveChangesAsync();
        }

        Task IDataContext.BulkDeleteAsync<T>(IQueryable<T> items)
        {
            this.RemoveRange(items);
            return this.SaveChangesAsync();
        }
    }
}
