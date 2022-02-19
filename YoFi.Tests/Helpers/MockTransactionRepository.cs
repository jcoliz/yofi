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

        public async Task AssignBankReferences()
        {
            var needbankrefs = All.Where(x => null == x.BankReference).ToList();

            // Doesn't need anyasync, this is a mock!!
            if (needbankrefs.Any())
            {
                foreach (var tx in needbankrefs)
                {
                    tx.GenerateBankReference();
                }
                await UpdateRangeAsync(needbankrefs);
            }
        }

        public Task<Stream> AsSpreadsheetAsync(int year, bool allyears, string q) => Task.FromResult(base.AsSpreadsheet());

        public Task BulkEditAsync(string category)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Split> CalculateCustomSplitRules(Transaction transaction, string json)
        {
            // We make splits for only one rule!
            if ("CategoryMatch" == json)
                return FakeObjects<Split>.Make(3);
            else
                return Enumerable.Empty<Split>();
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

        public override IQueryable<Transaction> ForQuery(string q) => All.AsQueryable();

        public Task<(Stream stream, string contenttype, string name)> GetReceiptAsync(Transaction transaction)
        {
            throw new NotImplementedException();
        }

        public Task<Transaction> GetWithSplitsByIdAsync(int? id) => base.GetByIdAsync(id);

        public Task UploadReceiptAsync(Transaction transaction, Stream stream, string contenttype)
        {
            throw new NotImplementedException();
        }
    }
}
