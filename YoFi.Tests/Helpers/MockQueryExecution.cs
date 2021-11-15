using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core;

namespace YoFi.Tests.Helpers
{
    public class MockQueryExecution : IAsyncQueryExecution
    {
        public Task<List<T>> ToListNoTrackingAsync<T>(IQueryable<T> query) where T : class
        {
            return Task.FromResult(query.ToList());
        }
    }
}
