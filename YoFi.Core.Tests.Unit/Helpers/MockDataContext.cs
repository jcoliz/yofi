using jcoliz.FakeObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core;
using System.Linq.Expressions;
using DocumentFormat.OpenXml.EMMA;

namespace YoFi.Tests.Helpers
{
    /// <summary>
    /// Provides an IDataProvider which can be totally controlled and inspected
    /// </summary>
    public class MockDataContext : IDataProvider, IFakeObjectsSaveTarget
    {
        public List<BudgetTx> BudgetTxData { get; } = new List<BudgetTx>();

        public List<Payee> PayeeData { get; } = new List<Payee>();

        public List<Transaction> TransactionData { get; } = new List<Transaction>();

        public List<Receipt> ReceiptData { get; } = new List<Receipt>();

        public List<Split> SplitData { get; } = new List<Split>();

        private IQueryable<Transaction> Transactions => TransactionData.AsQueryable();

        private IQueryable<Split> Splits => SplitData.AsQueryable();

        private IQueryable<Payee> Payees => PayeeData.AsQueryable();

        private IQueryable<BudgetTx> BudgetTxs => BudgetTxData.AsQueryable();

        public IQueryable<Transaction> TransactionsWithSplits => TransactionData.AsQueryable();

        public IQueryable<Split> SplitsWithTransactions
        {
            get
            {
                var result = new List<Split>();
                var txs = Transactions.Where(x => x.HasSplits);
                foreach (var tx in txs)
                {
                    foreach (var split in tx.Splits)
                    {
                        split.Transaction = tx;
                        result.Add(split);
                    }
                }

                return result.AsQueryable();
            }
        }

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
            if (typeof(T) == typeof(Split))
            {
                return Splits as IQueryable<T>;
            }
            if (typeof(T) == typeof(Receipt))
            {
                return ReceiptData.AsQueryable() as IQueryable<T>;
            }
            throw new NotImplementedException();
        }

        public IQueryable<TEntity> GetIncluding<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> _) where TEntity : class
        {
            if (typeof(TEntity) == typeof(Transaction))
            {
                return TransactionsWithSplits as IQueryable<TEntity>;
            }
            if (typeof(TEntity) == typeof(Split))
            {
                return SplitsWithTransactions as IQueryable<TEntity>;
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
            else if (item.GetType() == typeof(Receipt))
            {
                ReceiptData.Add(item as Receipt);
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
            else if (t == typeof(Split))
            {
                SplitData.AddRange(items as IEnumerable<Split>);
            }
            else if (t == typeof(Receipt))
            {
                ReceiptData.AddRange(items as IEnumerable<Receipt>);
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
            else if (t == typeof(Receipt))
            {
                var r = item as Receipt;

                var index = ReceiptData.FindIndex(x => x.ID == r.ID);
                ReceiptData.RemoveAt(index);
            }
            else if (t == typeof(Split))
            {
                var r = item as Split;

                var index = SplitData.FindIndex(x => x.ID == r.ID);
                SplitData.RemoveAt(index);
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

            if (PayeeData.Any())
            {
                var nextid = 1 + PayeeData.Max(x => x.ID);
                foreach (var tx in PayeeData.Where(x => x.ID == default))
                    tx.ID += nextid++;
            }

            if (BudgetTxData.Any())
            {
                var nextid = 1 + BudgetTxData.Max(x => x.ID);
                foreach (var tx in BudgetTxData.Where(x => x.ID == default))
                    tx.ID += nextid++;
            }
            if (ReceiptData.Any())
            {
                var nextid = 1 + ReceiptData.Max(x => x.ID);
                foreach (var tx in ReceiptData.Where(x => x.ID == default))
                    tx.ID += nextid++;
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
            else if (t == typeof(Receipt))
            {
                var theseitems = items as IEnumerable<Receipt>;
                ReceiptData.RemoveAll(x => theseitems.Contains(x));
            }
            else
                throw new NotImplementedException();
        }

        public void UpdateRange(IEnumerable<object> items)
        {
            foreach (var item in items)
                Update(item);
        }

        Task<List<T>> IDataProvider.ToListNoTrackingAsync<T>(IQueryable<T> query) => Task.FromResult(query.ToList());

        Task<int> IDataProvider.CountAsync<T>(IQueryable<T> query) => Task.FromResult(query.Count());

        Task<bool> IDataProvider.AnyAsync<T>(IQueryable<T> query) => Task.FromResult(query.Any());

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

        Task IDataProvider.BulkUpdateAsync<T>(IQueryable<T> items, T newvalues, List<string> columns)
        {
            // We support ONLY a very limited range of possibilities, which is where this
            // method is actually called.
            if (typeof(T) != typeof(Transaction))
                throw new NotImplementedException("Bulk Update on in-memory DB is only implemented for transactions");

            var txvalues = newvalues as Transaction;
            var txitems = items as IQueryable<Transaction>;
            var txlist = txitems.ToList();
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

            return Task.CompletedTask;
        }

        void IFakeObjectsSaveTarget.AddRange(System.Collections.IEnumerable objects)
        {
            var items = objects as IEnumerable<object>;
            this.AddRange(items);
        }
    }
}
