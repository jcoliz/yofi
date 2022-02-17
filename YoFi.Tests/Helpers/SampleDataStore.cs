using Common.NET.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.Core;
using YoFi.Core.Models;

namespace YoFi.Tests.Helpers
{
    public class SampleDataStore: IDataContext
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

        IQueryable<Transaction> IDataContext.Transactions => Transactions.AsQueryable();

        IQueryable<Transaction> IDataContext.TransactionsWithSplits => Transactions.AsQueryable();

        IQueryable<Split> IDataContext.Splits => Enumerable.Empty<Split>().AsQueryable();

        IQueryable<Split> IDataContext.SplitsWithTransactions
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

        IQueryable<Payee> IDataContext.Payees => throw new NotImplementedException();

        IQueryable<BudgetTx> IDataContext.BudgetTxs => BudgetTxs.AsQueryable();

        IQueryable<T> IDataContext.Get<T>()
        {
            throw new NotImplementedException();
        }

        void IDataContext.Add(object item)
        {
            throw new NotImplementedException();
        }

        void IDataContext.AddRange(IEnumerable<object> items)
        {
            throw new NotImplementedException();
        }

        void IDataContext.Update(object item)
        {
            throw new NotImplementedException();
        }

        void IDataContext.UpdateRange(IEnumerable<object> items)
        {
            throw new NotImplementedException();
        }

        void IDataContext.Remove(object item)
        {
            throw new NotImplementedException();
        }

        void IDataContext.RemoveRange(IEnumerable<object> items)
        {
            throw new NotImplementedException();
        }

        Task IDataContext.SaveChangesAsync()
        {
            throw new NotImplementedException();
        }

        Task<List<T>> IDataContext.ToListNoTrackingAsync<T>(IQueryable<T> query)
        {
            throw new NotImplementedException();
        }

        Task<int> IDataContext.CountAsync<T>(IQueryable<T> query)
        {
            throw new NotImplementedException();
        }

        Task<bool> IDataContext.AnyAsync<T>(IQueryable<T> query)
        {
            throw new NotImplementedException();
        }

        Task<int> IDataContext.ClearAsync<T>()
        {
            throw new NotImplementedException();
        }

        Task IDataContext.BulkInsertAsync<T>(IList<T> items)
        {
            throw new NotImplementedException();
        }

        Task IDataContext.BulkDeleteAsync<T>(IQueryable<T> items)
        {
            throw new NotImplementedException();
        }

        Task IDataContext.BulkUpdateAsync<T>(IQueryable<T> items, T newvalues, List<string> columns)
        {
            throw new NotImplementedException();
        }
    }
}
