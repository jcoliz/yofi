using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core;

namespace YoFi.Tests.Helpers
{
    /// <summary>
    /// Provides an IDataContext which can be totally controlled and inspected
    /// </summary>
    public class MockDataContext : IDataContext
    {
        public List<BudgetTx> BudgetTxData { get; } = new List<BudgetTx>();

        public List<Payee> PayeeData { get; } = new List<Payee>();

        public List<Transaction> TransactionData { get; } = new List<Transaction>();

        public IQueryable<Transaction> Transactions => TransactionData.AsQueryable();

        public IQueryable<Split> Splits => Enumerable.Empty<Split>().AsQueryable();

        public IQueryable<Payee> Payees => PayeeData.AsQueryable();

        public IQueryable<BudgetTx> BudgetTxs => BudgetTxData.AsQueryable();

        public IQueryable<Transaction> TransactionsWithSplits => TransactionData.AsQueryable();

        public IQueryable<Split> SplitsWithTransactions => throw new NotImplementedException();

        public IQueryable<T> Get<T>() where T: class
        {
            if (typeof(T) == typeof(BudgetTx))
            {
                return BudgetTxs as IQueryable<T>;
            }
            if (typeof(T) == typeof(Payee))
            {
                return Payees as IQueryable<T>;
            }
            if (typeof(T) == typeof(Transaction))
            {
                return Transactions as IQueryable<T>;
            }
            throw new NotImplementedException();
        }

        public void Add(object item)
        {
            if (item == null)
                throw new ArgumentException("Expected non-null item");

            if (item.GetType() == typeof(BudgetTx))
            {
                BudgetTxData.Add(item as BudgetTx);
            }
            else if (item.GetType() == typeof(Payee))
            {
                PayeeData.Add(item as Payee);
            }
            else if (item.GetType() == typeof(Transaction))
            {
                TransactionData.Add(item as Transaction);
            }
            else
                throw new NotImplementedException();
        }

        public void AddRange(IEnumerable<object> items)
        {
            if (items == null)
                throw new ArgumentException("Expected non-null items");

            if (!items.Any())
                return;

            var t = items.First().GetType();
            if (t == typeof(BudgetTx))
            {
                BudgetTxData.AddRange(items as IEnumerable<BudgetTx>);
            }
            else if (t == typeof(Payee))
            {
                PayeeData.AddRange(items as IEnumerable<Payee>);
            }
            else if (t == typeof(Transaction))
            {
                TransactionData.AddRange(items as IEnumerable<Transaction>);
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

            var t = item.GetType();
            if (t == typeof(BudgetTx))
            {
                var btx = item as BudgetTx;

                var index = BudgetTxData.FindIndex(x => x.ID == btx.ID);
                BudgetTxData.RemoveAt(index);
            }
            else if (t == typeof(Payee))
            {
                var btx = item as Payee;

                var index = PayeeData.FindIndex(x => x.ID == btx.ID);
                PayeeData.RemoveAt(index);
            }
            else if (t == typeof(Transaction))
            {
                var btx = item as Transaction;

                var index = TransactionData.FindIndex(x => x.ID == btx.ID);
                TransactionData.RemoveAt(index);
            }
            else
                throw new NotImplementedException();
        }

        public Task SaveChangesAsync()
        {
            // A lot of database tasks expect that items get IDs when committed to the DB.
            // We should try to mock that behaviour here.

            if (TransactionData.Any())
            {
                var nextid = 1 + TransactionData.Max(x => x.ID);
                foreach (var tx in TransactionData.Where(x => x.ID == default))
                    tx.ID += nextid++;

                nextid = 1 + TransactionData.Max(x => x.HasSplits ? x.Splits.Max(y => y.ID) : 0);
                foreach (var split in TransactionData.Where(x=>x.HasSplits).SelectMany(x => x.Splits).Where(x => x.ID == default))
                    split.ID += nextid++;
            }

            return Task.CompletedTask;
        }

        public void Update(object item)
        {
            if (item == null)
                throw new ArgumentException("Expected non-null item");

            var t = item.GetType();
            if (t == typeof(BudgetTx))
            {
                var btx = item as BudgetTx;

                var index = BudgetTxData.FindIndex(x => x.ID == btx.ID);
                BudgetTxData[index] = btx;
            }
            else if (t == typeof(Payee))
            {
                var btx = item as Payee;

                var index = PayeeData.FindIndex(x => x.ID == btx.ID);
                PayeeData[index] = btx;
            }
            else if (t == typeof(Transaction))
            {
                var btx = item as Transaction;

                var index = TransactionData.FindIndex(x => x.ID == btx.ID);
                TransactionData[index] = btx;
            }
            else
                throw new NotImplementedException();
        }

        public void RemoveRange(IEnumerable<object> items)
        {
            if (items == null)
                throw new ArgumentException("Expected non-null items");

            if (!items.Any())
                return;

            var t = items.First().GetType();
            if (t == typeof(Payee))
            {
                var theseitems = items as IEnumerable<Payee>;
                PayeeData.RemoveAll(x=>theseitems.Contains(x));
            }
            else if (t == typeof(BudgetTx))
            {
                var theseitems = items as IEnumerable<BudgetTx>;
                BudgetTxData.RemoveAll(x => theseitems.Contains(x));
            }
            else if (t == typeof(Transaction))
            {
                var theseitems = items as IEnumerable<Transaction>;
                TransactionData.RemoveAll(x => theseitems.Contains(x));
            }
            else
                throw new NotImplementedException();
        }

        public void UpdateRange(IEnumerable<object> items)
        {
            foreach (var item in items)
                Update(item);
        }

        Task<List<T>> IDataContext.ToListNoTrackingAsync<T>(IQueryable<T> query) => Task.FromResult(query.ToList());

        Task<int> IDataContext.CountAsync<T>(IQueryable<T> query) => Task.FromResult(query.Count());

        Task<bool> IDataContext.AnyAsync<T>(IQueryable<T> query) => Task.FromResult(query.Any());

        public Task<int> ClearAsync<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public Task BulkInsertAsync<T>(IList<T> items) where T : class
        {
            return AddRangeAsync(items);
        }

        public Task BulkDeleteAsync<T>(IQueryable<T> items) where T : class
        {
            RemoveRange(items);
            return Task.CompletedTask;
        }

        Task IDataContext.BulkUpdateAsync<T>(IQueryable<T> items, T newvalues, List<string> columns)
        {
            throw new NotImplementedException();
        }
    }
}
