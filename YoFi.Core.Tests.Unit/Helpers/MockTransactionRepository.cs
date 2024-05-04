using jcoliz.FakeObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.Tests.Helpers
{
    public class MockTransactionRepository : BaseMockRepository<Transaction>, ITransactionRepository, IFakeObjectsSaveTarget
    {
        public IQueryable<Split> Splits => throw new NotImplementedException();

        public IStorageService Storage {get;set;}

        public void AddRange(IEnumerable objects)
        {
            base.AddRangeAsync(objects as IEnumerable<Transaction>).Wait();
        }

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

        public Task BulkInsertWithSplitsAsync(IList<Transaction> items)
        {
            return base.BulkInsertAsync(items);
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
        public Task SetHiddenAsync(int id, bool value) => throw new NotImplementedException();
        public Task SetSelectedAsync(int id, bool value) => throw new NotImplementedException();

        public async Task UploadReceiptAsync(Transaction transaction, Stream stream, string contenttype)
        {
            //
            // Save the file to blob storage
            //
            // TODO: Consolodate this with the exact same copy which is in ApiController
            //

            // Note that the view should not ever get this far. It's the view's reposibility to check first if
            // there is storage defined. Ergo, if we get this far, it's a legit 500 error.
            if (null == Storage)
                throw new ApplicationException("Storage is not defined");

            string blobname = transaction.ID.ToString();

            await Storage.UploadBlobAsync(blobname, stream, contenttype);

            // Save it in the Transaction
            // If there was a problem, UploadToBlob will throw an exception.

            transaction.ReceiptUrl = blobname;
            await UpdateAsync(transaction);
        }
    }
}
