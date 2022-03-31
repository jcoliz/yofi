using jcoliz.FakeObjects;
using System.Linq.Expressions;
using YoFi.Core;
using YoFi.Core.Models;

namespace YoFi.Experiments.WebApi.Data;
public class MockDataContext : IDataContext
{
    public void Add(object item)
    {
        // Add an ID to it
        if (item is Transaction tx)
        {
            tx.ID = 11;
        }
    }

    public void AddRange(IEnumerable<object> items)
    {
        throw new NotImplementedException();
    }

    public Task<bool> AnyAsync<T>(IQueryable<T> query) where T : class
    {
        return Task.FromResult(query.Any());
    }

    public Task BulkDeleteAsync<T>(IQueryable<T> items) where T : class
    {
        throw new NotImplementedException();
    }

    public Task BulkInsertAsync<T>(IList<T> items) where T : class
    {
        throw new NotImplementedException();
    }

    public Task BulkUpdateAsync<T>(IQueryable<T> items, T newvalues, List<string> columns) where T : class
    {
        throw new NotImplementedException();
    }

    public Task<int> ClearAsync<T>() where T : class
    {
        throw new NotImplementedException();
    }

    public Task<int> CountAsync<T>(IQueryable<T> query) where T : class
    {
        return Task.FromResult(query.Count());
    }

    public IQueryable<T> Get<T>() where T : class
    {
        if (typeof(T) == typeof(Transaction))
        {
            int id = 1;
            return FakeObjects<Transaction>.Make(10,x=>x.ID = id++).AsQueryable() as IQueryable<T> ?? Enumerable.Empty<T>().AsQueryable();
        }
        throw new NotImplementedException();
    }

    public IQueryable<TEntity> GetIncluding<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath) where TEntity : class
    {
        if (typeof(TEntity) == typeof(Transaction))
        {
            int id = 1;
            return FakeObjects<Transaction>.Make(10,x=>x.ID = id++).AsQueryable() as IQueryable<TEntity> ?? Enumerable.Empty<TEntity>().AsQueryable();
        }
        throw new NotImplementedException();
    }

    public void Remove(object item)
    {
        // NOP
    }

    public void RemoveRange(IEnumerable<object> items)
    {
        throw new NotImplementedException();
    }

    public Task SaveChangesAsync()
    {
        // NOP
        return Task.CompletedTask;
    }

    public Task<List<T>> ToListNoTrackingAsync<T>(IQueryable<T> query) where T : class
    {
        return Task.FromResult(query.ToList());
    }

    public void Update(object item)
    {
        throw new NotImplementedException();
    }

    public void UpdateRange(IEnumerable<object> items)
    {
        throw new NotImplementedException();
    }
}