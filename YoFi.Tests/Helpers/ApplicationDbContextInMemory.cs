using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Data;
using YoFi.Core;
using YoFi.Core.Models;

namespace YoFi.Tests.Helpers
{
    /// <summary>
    /// IN-memory version of application context
    /// </summary>
    /// <remarks>
    /// There are a few things we can't do in an inmemory DB, so we work around those here.
    /// </remarks>
    public class ApplicationDbContextInMemory : ApplicationDbContext, IDataProvider
    {
        public ApplicationDbContextInMemory(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        async Task IDataProvider.BulkInsertAsync<T>(IList<T> items)
        {
            await this.AddRangeAsync(items);
            await this.SaveChangesAsync();
        }

        Task IDataProvider.BulkDeleteAsync<T>(IQueryable<T> items)
        {
            this.RemoveRange(items);
            return this.SaveChangesAsync();
        }

        async Task IDataProvider.BulkUpdateAsync<T>(IQueryable<T> items, T newvalues, List<string> columns)
        {
            // We support ONLY a very limited range of possibilities, which is where this
            // method is actually called.
            if (typeof(T) != typeof(Transaction))
                throw new NotImplementedException("Bulk Update on in-memory DB is only implemented for transactions");

            var txvalues = newvalues as Transaction;
            var txitems = items as IQueryable<Transaction>;
            var txlist = await txitems.ToListAsync();
            foreach (var item in txlist)
            {
                if (columns.Contains("Imported"))
                    item.Imported = txvalues.Imported;
                if (columns.Contains("Hidden"))
                    item.Hidden = txvalues.Hidden;
                if (columns.Contains("Selected"))
                    item.Selected = txvalues.Selected;
            }
            UpdateRange(txlist);

            await SaveChangesAsync();
        }
    }
}
