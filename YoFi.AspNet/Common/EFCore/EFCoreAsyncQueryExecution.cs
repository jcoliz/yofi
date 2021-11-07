using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Core;

namespace Common.EFCore
{
    public class EFCoreAsyncQueryExecution : IAsyncQueryExecution
    {
        public Task<List<T>> ToListNoTrackingAsync<T>(IQueryable<T> query) where T: class
        {
            return query.AsNoTracking().ToListAsync();
        }
    }
}
