using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.Tests.Helpers
{
    public class MockTransactionRepository : BaseMockRepository<Transaction>, ITransactionRepository
    {
        public IQueryable<Split> Splits => throw new NotImplementedException();

        public Task<int> AddSplitToAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> AsSpreadsheetAsync(int year, bool allyears, string q)
        {
            throw new NotImplementedException();
        }

        public Task BulkEditAsync(string category)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Split> CalculateCustomSplitRules(Transaction transaction, string json)
        {
            throw new NotImplementedException();
        }

        public Task CancelImportAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> CategoryAutocompleteAsync(string q)
        {
            throw new NotImplementedException();
        }

        public Task FinalizeImportAsync()
        {
            throw new NotImplementedException();
        }

        public override IQueryable<Transaction> ForQuery(string q)
        {
            throw new NotImplementedException();
        }

        public Task<(Stream stream, string contenttype, string name)> GetReceiptAsync(Transaction transaction)
        {
            throw new NotImplementedException();
        }

        public Task<Transaction> GetWithSplitsByIdAsync(int? id)
        {
            throw new NotImplementedException();
        }

        public override Transaction MakeItem(int x) =>
            new Transaction() { ID = x, Payee = x.ToString(), Category = x.ToString(), Amount = x * 100m, Timestamp = new DateTime(2001, 1, 1) + TimeSpan.FromDays(x) };

        public Task UploadReceiptAsync(Transaction transaction, Stream stream, string contenttype)
        {
            throw new NotImplementedException();
        }
    }
}
