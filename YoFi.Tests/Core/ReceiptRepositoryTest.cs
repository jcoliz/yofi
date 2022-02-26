#define RECEIPTSINDB

using Common.DotNet;
using Common.DotNet.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoFi.Core;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class ReceiptRepositoryTest: IFakeObjectsSaveTarget
    {
        #region Fields

        IReceiptRepository repository;
        ITransactionRepository txrepo;
        TestAzureStorage storage;
        TestClock clock;
        const string contenttype = "image/png";
#if RECEIPTSINDB
        IDataContext context;
#endif
        #endregion

        [TestInitialize]
        public void SetUp()
        {
            storage = new TestAzureStorage();
            txrepo = new MockTransactionRepository() { Storage = storage };
            
            // Match clock with fakeobjectsmaker            
            clock = new TestClock() { Now = new DateTime(2001, 12, 31) };

            //
            // There are two competing implementations of IReceiptRepository. One persists the receipt metadata in
            // the database. That's ReceiptRepositoryInDb. The other uses only the azure storage as its source of
            // truth. That's ReceiptRepository. I am debating which is better. I am leaning toward the DB version.
            //

#if RECEIPTSINDB
            context = new ReceiptDataContext();
            repository = new ReceiptRepositoryInDb(context, txrepo, storage, clock);
#else
            repository = new ReceiptRepository(txrepo,storage,clock);
#endif
        }

        #region Helpers

        public void AddRange(System.Collections.IEnumerable objects)
        {
            if (objects is IEnumerable<Transaction> txs)
            {
                txrepo.AddRangeAsync(txs).Wait();
            }
        }
        private void GivenReceiptInStorage(string filename)
        {

#if RECEIPTSINDB
            var item = Receipt.FromFilename(filename, clock: clock);
            context.Add(item);
            storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = $"{ReceiptRepositoryInDb.Prefix}{item.ID}", InternalFile = "budget-white-60x.png", ContentType = contenttype });
#else
            storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = ReceiptRepository.Prefix + filename, InternalFile = "budget-white-60x.png", ContentType = contenttype });
#endif
        }

        private void GivenMultipleReceipts(Transaction t)
        {
            // And: One receipt in storage, which will match the transaction we care about
            var filename = $"{t.Payee} ${t.Amount}.png";
            GivenReceiptInStorage(filename);

            // And: One receipt in storage, which will match MANY transactions
            filename = $"Payee.png";
            GivenReceiptInStorage(filename);

            // And: One receipt in storage, which will NOT MATCH ANY transactions
            filename = $"Totally not matching.png";
            GivenReceiptInStorage(filename);
        }

        #endregion

        #region Tests

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(repository);
        }

        [TestMethod]
        public async Task Upload()
        {
            // Given: A receipt file
            var length = 25;
            var stream = new MemoryStream(Enumerable.Repeat<byte>(0x60, length).ToArray());

            // When: Uploading it to the repository
            var filename = "Uptown Espresso $5.11 1-2.png";
            await repository.UploadReceiptAsync(filename, stream, contenttype);

            // Then : Repository has it
            var items = await repository.GetAllAsync();
            Assert.AreEqual(1, items.Count());

            // Then: The receipt is contained in storage
            Assert.AreEqual(1, storage.BlobItems.Count());
            Assert.AreEqual(contenttype, storage.BlobItems.Single().ContentType);
#if RECEIPTSINDB
            Assert.AreEqual($"{ReceiptRepositoryInDb.Prefix}{items.Single().ID}", storage.BlobItems.Single().FileName);
#else
            Assert.AreEqual(ReceiptRepository.Prefix + filename, storage.BlobItems.Single().FileName);
#endif
        }

        [TestMethod]
        public async Task GetNone()
        {
            // Given: Empty storage

            // When: Getting All
            var items = await repository.GetAllAsync();

            // Then: Nothing returned
            Assert.IsFalse(items.Any());
        }

        [TestMethod]
        public async Task GetOne()
        {
            // Given: One receipt in storage
            var filename = "Uptown Espresso $5.11 1-2.png";
            GivenReceiptInStorage(filename);  

            // When: Getting All
            var items = await repository.GetAllAsync();

            // Then: One item returned
            Assert.AreEqual(1, items.Count());

            // And: It was matched correctly
            var actual = items.Single();
            Assert.AreEqual("Uptown Espresso", actual.Name);
            Assert.AreEqual(new DateTime(2001,1,2), actual.Timestamp);
        }

        [TestMethod]
        public async Task GetMany()
        {
            // Given: Many receipts in storage
            for(int i=1; i<10; i++ )
            {
                var filename = $"Uptown Espresso $5.11 1-{i}.png";
                GivenReceiptInStorage(filename);
            }

            // When: Getting All
            var items = await repository.GetAllAsync();

            // Then: Nine items returned
            Assert.AreEqual(9, items.Count());

            // And: Names matched correctly
            Assert.IsTrue(items.All(x=>x.Name =="Uptown Espresso"));
        }

        [TestMethod]
        public async Task GetOneTransaction()
        {
            // TODO: This make me realize that the TransactionsForReceipts narrower may be incorrect.
            // If there is an amount in the receipt, then it will REQUIRE the receipt to have that amount
            // That's probably not right, because it's still possible to match, with a lesser match score,
            // even if the amounts are wrong.

            // Given: One transaction
            var tx = FakeObjects<Transaction>.Make(1).SaveTo(this).Single();

            // And: One receipt in storage which will match that
            var filename = $"{tx.Payee} {tx.Timestamp.ToString("MM-dd")}.png";
            GivenReceiptInStorage(filename);

            // When: Getting All
            var items = await repository.GetAllAsync();

            // Then: The transaction is listed among the matches
            var actual = items.Single();
            Assert.AreEqual(tx, actual.Matches.Single());
        }

        [TestMethod]
        public async Task GetManyTransactions()
        {
            // TODO: This test makes me realize that TransactionsForReceipts narrower is also wrong
            // regarding dates. It needs to employ the same +/- rangefinfer that "transactionmatches" uses

            // Given: Many transactions
            var txs = FakeObjects<Transaction>.Make(10).SaveTo(this).Group(0);

            // And: One receipt in storage which will match ALL of those
            var filename = $"Payee {txs[5].Timestamp.ToString("MM-dd")}.png";
            GivenReceiptInStorage(filename);

            // When: Getting All
            var items = await repository.GetAllAsync();

            // Then: Alls transactions are listed among the matches
            var actual = items.Single();
            Assert.IsTrue(actual.Matches.OrderBy(TestKey<Transaction>.Order()).SequenceEqual(txs));
        }

        [TestMethod]
        public async Task AssignReceipt()
        {
            // Given: One receipt in storage
            var filename = "Uptown Espresso $5.11 1-2.png";
            GivenReceiptInStorage(filename);

            // And: Getting it
            var items = await repository.GetAllAsync();
            var r = items.Single();

            // And: A transaction (doesn't matter if it's matching
            var t = FakeObjects<Transaction>.Make(1).SaveTo(this).Single();

            // When: Assigning the receipt to the transaction
            
            await repository.AssignReceipt(r, t);

            // Then: The transaction displays as having a receipt
            Assert.IsFalse(string.IsNullOrEmpty(t.ReceiptUrl));

            // And: The receipt is contained in storage as expected
#if RECEIPTSINDB
            var blob = storage.BlobItems.Where(x => x.FileName == ReceiptRepositoryInDb.Prefix + r.ID.ToString()).Single();
#else
            var blob = storage.BlobItems.Where(x=>x.FileName == t.ID.ToString()).Single();
#endif
            Assert.AreEqual(contenttype,blob.ContentType);

            // And: There are no more (unassigned) receipts now
            items = await repository.GetAllAsync();
            Assert.IsFalse(items.Any());
        }

        [TestMethod]
        public async Task AssignReceiptAllMatch()
        {
            // Given: Several transactions, one of which we care about
            var t = FakeObjects<Transaction>.Make(10).SaveTo(this).Last();

            // And: One receipt in storage, which will match the transaction we care about
            var filename = $"{t.Payee} ${t.Amount}.png";
            GivenReceiptInStorage(filename);

            // When: Assigning the receipt to its best match
            var matched = await repository.AssignAll();

            // Then: One receipt was matched
            Assert.AreEqual(1, matched);

            // Then: The selected transaction displays as having a receipt
            Assert.IsFalse(string.IsNullOrEmpty(t.ReceiptUrl));

            // And: The receipt is contained in storage as expected
#if RECEIPTSINDB
            var blob = storage.BlobItems.Where(x => x.FileName == $"{ReceiptRepositoryInDb.Prefix}1").Single();
#else
            var blob = storage.BlobItems.Where(x=>x.FileName == t.ID.ToString()).Single();
#endif
            Assert.AreEqual(contenttype, blob.ContentType);

            // And: There are no more (unassigned) receipts now
            var items = await repository.GetAllAsync();
            Assert.IsFalse(items.Any());
        }

        [TestMethod]
        public async Task AssignAllNoMatch()
        {
            // Given: Several transactions
            _ = FakeObjects<Transaction>.Make(10).SaveTo(this);

            // And: One receipt in storage, which will NOT MATCH ANY transactions
            var filename = $"Totally not matching.png";
            GivenReceiptInStorage(filename);

            // When: Assigning the receipt to its best match
            var matched = await repository.AssignAll();

            // Then: No receipts were matched
            Assert.AreEqual(0, matched);

            // And: There is still just one (unassigned) receipt now
            var items = await repository.GetAllAsync();
            Assert.AreEqual(1,items.Count());
        }

        [TestMethod]
        public async Task AssignAllManyMatch()
        {
            // Given: Several transactions
            _ = FakeObjects<Transaction>.Make(10).SaveTo(this);

            // And: One receipt in storage, which will match MANY transactions
            var filename = $"Payee.png";
            GivenReceiptInStorage(filename);

            // When: Assigning the receipt to its best match
            var matched = await repository.AssignAll();

            // Then: No receipts were matched
            Assert.AreEqual(0, matched);

            // And: There is still just one (unassigned) receipt now
            var items = await repository.GetAllAsync();
            Assert.AreEqual(1, items.Count());
        }

        [TestMethod]
        public async Task AssignAllVariousMatch()
        {
            // Given: Several transactions, one of which we care about
            var t = FakeObjects<Transaction>.Make(10).SaveTo(this).Last();

            // And: Multiple receipts, one matches this transaction, one matches all, one matches none
            GivenMultipleReceipts(t);

            // When: Assigning the receipt to its best match
            var matched = await repository.AssignAll();

            // Then: One receipt was matched
            Assert.AreEqual(1, matched);

            // And: There are two (unassigned) receipts now
            var items = await repository.GetAllAsync();
            Assert.AreEqual(2, items.Count());
        }

        [TestMethod]
        public async Task Delete()
        {
            // Given: One receipt in storage
            var filename = "Uptown Espresso $5.11 1-2.png";
            GivenReceiptInStorage(filename);

            // And: Getting All
            var items = await repository.GetAllAsync();

            // When: Deleting it
            await repository.DeleteAsync(items.Single());

            // And: Getting All again
            items = await repository.GetAllAsync();

            // Then: Nothing returned
            Assert.IsFalse(items.Any());
        }

        [TestMethod]
        public async Task DeleteFromNone()
        {
            // Given: No receipts

            // When: Deleting a nonexistent receipt
            var filename = "Uptown Espresso $5.11 1-2.png";
            await repository.DeleteAsync(new Receipt() { ID = 100, Filename = ReceiptRepository.Prefix + filename });

            // Then: Fails silently
        }

        [TestMethod]
        public async Task GetMatchingOne()
        {
            // Given: Several transactions, one of which we care about
            var t = FakeObjects<Transaction>.Make(10).SaveTo(this).Last();

            // And: Multiple receipts, one matches this transaction, one matches all, one matches none
            GivenMultipleReceipts(t);

            // When: Asking for the receipt(s) that matches THIS transaction
            var matches = await repository.GetMatchingAsync(t);

            // Then: There are two matches
            Assert.AreEqual(2,matches.Count());

            // And: The first one is the best one
            var best = matches.First();
            Assert.AreEqual(t.Payee, best.Name);
        }

        [TestMethod]
        public async Task EndToEnd()
        {
            // Given: A real tx repository, and a receipt repository build off that
            var mockdc = new MockDataContext();
            txrepo = new TransactionRepository(mockdc,clock,storage);
#if RECEIPTSINDB
            repository = new ReceiptRepositoryInDb(context, txrepo, storage, clock);
#else
            repository = new ReceiptRepository(txrepo, storage,clock);
#endif

            // Given: Several transactions, one of which we care about
            var t = FakeObjects<Transaction>.Make(10).SaveTo(this).Last();

            // And: Multiple receipts, one matches this transaction, one matches all, one matches none
            GivenMultipleReceipts(t);

            // And: Assigning the receipts to their top match
            var matched = await repository.AssignAll();

            // When: Downloading transaction for receipt
            (var stream, var contenttype, var name) = await txrepo.GetReceiptAsync(t);

            // Then: File details are as expected
            Assert.AreEqual("image/png", contenttype);
            Assert.AreEqual(4561, stream.Length);

#if RECEIPTSINDB

            // And: The receipt name identifies a receipt which is no longer in the system
            // (because we deleted it)
            var namematch = new Regex($"^{ReceiptRepositoryInDb.Prefix}(?<id>[0-9]+)$");
            var match = namematch.Match(name);
            Assert.IsTrue(match.Success);

            var id = int.Parse(match.Groups["id"].Value);
            Assert.IsFalse((await repository.GetAllAsync()).Where(x=>x.ID == id).Any());
#endif
        }

        #endregion

    }

    internal class ReceiptDataContext : IDataContext
    {
        int nextid = 1;

        public List<Receipt> Receipts { get; } = new List<Receipt>();

        public IQueryable<Transaction> Transactions => throw new NotImplementedException();

        public IQueryable<Transaction> TransactionsWithSplits => throw new NotImplementedException();

        public IQueryable<Split> Splits => throw new NotImplementedException();

        public IQueryable<Split> SplitsWithTransactions => throw new NotImplementedException();

        public IQueryable<Payee> Payees => throw new NotImplementedException();

        public IQueryable<BudgetTx> BudgetTxs => throw new NotImplementedException();

        public void Add(object item)
        {
            var receipt = item as Receipt; 
            receipt.ID = nextid++;
            Receipts.Add(receipt);
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
            throw new NotImplementedException();
        }

        public IQueryable<T> Get<T>() where T : class
        {
            return Receipts.AsQueryable() as IQueryable<T>;
        }

        public void Remove(object item)
        {
            Receipts.Remove(item as Receipt);
        }

        public void RemoveRange(IEnumerable<object> items)
        {
            throw new NotImplementedException();
        }

        public Task SaveChangesAsync()
        {
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
}
