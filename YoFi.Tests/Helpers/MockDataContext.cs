using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Models;
using YoFi.Core;

namespace YoFi.Tests.Helpers
{
    /// <summary>
    /// Provides an IDataContext which can be totally controlled and inspected
    /// </summary>
    public class MockDataContext : IDataContext
    {
        public List<BudgetTx> BudgetTxData { get; } = new List<BudgetTx>();

        public IQueryable<Transaction> Transactions => throw new NotImplementedException();

        public IQueryable<Split> Splits => throw new NotImplementedException();

        public IQueryable<Payee> Payees => throw new NotImplementedException();

        public IQueryable<BudgetTx> BudgetTxs => BudgetTxData.AsQueryable();

        public IQueryable<T> Get<T>()
        {
            if (typeof(T) == typeof(BudgetTx))
            {
                return BudgetTxs as IQueryable<T>;
            }
            else
                throw new NotImplementedException();
        }

        public Task AddAsync(object item)
        {
            if (item == null)
                throw new ArgumentException("Expected non-null item");

            if (item.GetType() == typeof(BudgetTx))
            {
                BudgetTxData.Add(item as BudgetTx);
            }
            else
                throw new NotImplementedException();

            return Task.CompletedTask;
        }

        public void AddRange(IEnumerable<object> items)
        {
            if (items == null)
                throw new ArgumentException("Expected non-null items");

            if (!items.Any())
                return;

            if (items.First().GetType() == typeof(BudgetTx))
            {
                BudgetTxData.AddRange(items as IEnumerable<BudgetTx>);
            }
            else
                throw new NotImplementedException();
        }

        public Task AddRangeAsync(IEnumerable<object> items)
        {
            AddRange(items);
            return Task.CompletedTask;
        }

        public void Remove(object item)
        {
            if (item == null)
                throw new ArgumentException("Expected non-null item");

            if (item.GetType() == typeof(BudgetTx))
            {
                var btx = item as BudgetTx;

                var index = BudgetTxData.FindIndex(x => x.ID == btx.ID);
                BudgetTxData.RemoveAt(index);
            }
            else
                throw new NotImplementedException();
        }

        public Task SaveChangesAsync()
        {
            return Task.CompletedTask;
        }

        public void Update(object item)
        {
            if (item == null)
                throw new ArgumentException("Expected non-null item");

            if (item.GetType() == typeof(BudgetTx))
            {
                var btx = item as BudgetTx;

                var index = BudgetTxData.FindIndex(x => x.ID == btx.ID);
                BudgetTxData[index] = btx;
            }
            else
                throw new NotImplementedException();
        }
    }
}
