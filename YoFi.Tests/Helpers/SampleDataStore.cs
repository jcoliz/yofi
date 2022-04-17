using Common.DotNet.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.Core;
using YoFi.Core.Models;

namespace YoFi.Tests.Helpers
{
    public class SampleDataStore: IDataProvider
    {
        public List<Transaction> Transactions { get; set; }

        public List<Payee> Payees { get; set; }

        public List<BudgetTx> BudgetTxs { get; set; }

        public async Task SerializeAsync(Stream stream)
        {
            await JsonSerializer.SerializeAsync<SampleDataStore>(stream, this);
        }

        public async Task DeSerializeAsync(Stream stream)
        {
            var incoming = await JsonSerializer.DeserializeAsync<SampleDataStore>(stream);

            Transactions = incoming.Transactions;
            Payees = incoming.Payees;
            BudgetTxs = incoming.BudgetTxs;
        }

        public static readonly string FileName = "FullSampleData.json";
        public static SampleDataStore Single = null;

        public static async Task LoadSingleAsync()
        {
            if (null == Single)
            {
                var stream = SampleData.Open(FileName);
                Single = await JsonSerializer.DeserializeAsync<SampleDataStore>(stream);
            }

            // Otherwise problems occur in back-to-back tests with same data
            foreach (var tx in Single.Transactions)
            {
                tx.ID = default;
                if (tx.HasSplits)
                    foreach (var s in tx.Splits)
                        s.ID = default;
                else
                    tx.Splits = new List<Split>();
            }
        }


        private IQueryable<Transaction> TransactionsWithSplits => Transactions.AsQueryable();

        private IQueryable<Split> SplitsWithTransactions
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

        IQueryable<T> IDataProvider.Get<T>()
        {
            if (typeof(T) == typeof(BudgetTx))
            {
                return BudgetTxs.AsQueryable() as IQueryable<T>;
            }
            if (typeof(T) == typeof(Transaction))
            {
                return Transactions.AsQueryable() as IQueryable<T>;
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

        void IDataProvider.Add(object item)
        {
            throw new NotImplementedException();
        }

        void IDataProvider.AddRange(IEnumerable<object> items)
        {
            throw new NotImplementedException();
        }

        void IDataProvider.Update(object item)
        {
            throw new NotImplementedException();
        }

        void IDataProvider.UpdateRange(IEnumerable<object> items)
        {
            throw new NotImplementedException();
        }

        void IDataProvider.Remove(object item)
        {
            throw new NotImplementedException();
        }

        void IDataProvider.RemoveRange(IEnumerable<object> items)
        {
            throw new NotImplementedException();
        }

        Task IDataProvider.SaveChangesAsync()
        {
            throw new NotImplementedException();
        }

        Task<List<T>> IDataProvider.ToListNoTrackingAsync<T>(IQueryable<T> query)
        {
            throw new NotImplementedException();
        }

        Task<int> IDataProvider.CountAsync<T>(IQueryable<T> query)
        {
            throw new NotImplementedException();
        }

        Task<bool> IDataProvider.AnyAsync<T>(IQueryable<T> query)
        {
            throw new NotImplementedException();
        }

        Task<int> IDataProvider.ClearAsync<T>()
        {
            throw new NotImplementedException();
        }

        Task<bool> IDataProvider.BulkInsertAsync<T>(IList<T> items)
        {
            throw new NotImplementedException();
        }

        Task IDataProvider.BulkDeleteAsync<T>(IQueryable<T> items)
        {
            throw new NotImplementedException();
        }

        Task IDataProvider.BulkUpdateAsync<T>(IQueryable<T> items, T newvalues, List<string> columns)
        {
            throw new NotImplementedException();
        }
    }
}
