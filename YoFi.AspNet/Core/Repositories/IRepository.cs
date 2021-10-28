using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core.Repositories
{
    public interface IRepository<T> where T: class
    {
        IQueryable<T> All { get; }
        IQueryable<T> OrderedQuery { get; }
        IQueryable<T> InDefaultOrder(IQueryable<T> original);
        IQueryable<T> ForQuery(string q);
        Task<T> GetByIdAsync(int? id);
        Task<bool> TestExistsByIdAsync(int id);
        Task AddAsync(T item);
        Task AddRangeAsync(IEnumerable<T> items);
        Task UpdateAsync(T item);
        Task RemoveAsync(T item);
        Stream AsSpreadsheet();
    }
}
