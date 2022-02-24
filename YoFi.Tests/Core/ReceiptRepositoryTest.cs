using Common.DotNet;
using Common.DotNet.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class ReceiptRepositoryTest
    {
        ReceiptRepository repository;
        MockTransactionRepository txrepo;
        TestAzureStorage storage;
        TestClock clock;

        [TestInitialize]
        public void SetUp()
        {
            storage = new TestAzureStorage();
            txrepo = new MockTransactionRepository() { Storage = storage };
            
            // Match clock with fakeobjectsmaker
            
            clock = new TestClock() { Now = new DateTime(2001, 12, 31) }; 
            repository = new ReceiptRepository(txrepo,storage,clock);
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(repository);
        }

        [TestMethod]
        public async Task Upload()
        {
            // Given: A receipt file
            var contenttype = "image/png";
            var length = 25;
            var stream = new MemoryStream(Enumerable.Repeat<byte>(0x60, length).ToArray());

            // When: Uploading it to the repository
            var filename = "Uptown Espresso $5.11 1-2.png";
            await repository.UploadReceiptAsync(filename, stream, contenttype);

            // Then: The receipt is contained in storage
            Assert.AreEqual(1, storage.BlobItems.Count());
            Assert.AreEqual(contenttype, storage.BlobItems.Single().ContentType);
            Assert.AreEqual("receipt/"+filename, storage.BlobItems.Single().FileName);
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
            var contenttype = "image/png";
            storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = "receipt/" + filename, InternalFile = "budget-white-60x.png", ContentType = contenttype });

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
            var contenttype = "image/png";
            for(int i=1; i<10; i++ )
            {
                var filename = $"Uptown Espresso $5.11 1-{i}.png";
                storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = "receipt/" + filename, InternalFile = "budget-white-60x.png", ContentType = contenttype });
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
            var tx = FakeObjects<Transaction>.Make(1).SaveTo(txrepo).Single();

            // And: One receipt in storage which will match that
            var filename = $"{tx.Payee} {tx.Timestamp.ToString("MM-dd")}.png";
            var contenttype = "image/png";
            storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = "receipt/" + filename, InternalFile = "budget-white-60x.png", ContentType = contenttype });

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
            var txs = FakeObjects<Transaction>.Make(10).SaveTo(txrepo).Group(0);

            // And: One receipt in storage which will match ALL of those
            var filename = $"Payee {txs[5].Timestamp.ToString("MM-dd")}.png";
            var contenttype = "image/png";
            storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = "receipt/" + filename, InternalFile = "budget-white-60x.png", ContentType = contenttype });

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
            var contenttype = "image/png";
            storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = "receipt/" + filename, InternalFile = "budget-white-60x.png", ContentType = contenttype });

            // And: Getting it
            var items = await repository.GetAllAsync();
            var r = items.Single();

            // And: A transaction (doesn't matter if it's matching
            var t = FakeObjects<Transaction>.Make(1).SaveTo(txrepo).Single();

            // When: Assigning the receipt to the transaction
            
            await repository.AssignReceipt(r, t);

            // Then: The transaction displays as having a receipt
            Assert.IsFalse(string.IsNullOrEmpty(t.ReceiptUrl));

            // And: The receipt is contained in storage as expected
            var blob = storage.BlobItems.Where(x=>x.FileName == t.ID.ToString()).Single();
            Assert.AreEqual(contenttype,blob.ContentType);

            // And: There are no more (unassigned) receipts now
            items = await repository.GetAllAsync();
            Assert.IsFalse(items.Any());
        }

        [TestMethod]
        public async Task AssignReceiptAllMatch()
        {
            // Given: Several transactions, one of which we care about
            var t = FakeObjects<Transaction>.Make(10).SaveTo(txrepo).Last();

            // And: One receipt in storage, which will match the transaction we care about
            var filename = $"{t.Payee} ${t.Amount}.png";
            var contenttype = "image/png";
            storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = "receipt/" + filename, InternalFile = "budget-white-60x.png", ContentType = contenttype });

            // When: Assigning the receipt to its best match
            var matched = await repository.AssignAll();

            // Then: One receipt was matched
            Assert.AreEqual(1, matched);

            // Then: The selected transaction displays as having a receipt
            Assert.IsFalse(string.IsNullOrEmpty(t.ReceiptUrl));

            // And: The receipt is contained in storage as expected
            var blob = storage.BlobItems.Where(x => x.FileName == t.ID.ToString()).Single();
            Assert.AreEqual(contenttype, blob.ContentType);

            // And: There are no more (unassigned) receipts now
            var items = await repository.GetAllAsync();
            Assert.IsFalse(items.Any());
        }

        [TestMethod]
        public async Task AssignAllNoMatch()
        {
            // Given: Several transactions
            _ = FakeObjects<Transaction>.Make(10).SaveTo(txrepo);

            // And: One receipt in storage, which will NOT MATCH ANY transactions
            var filename = $"Totally not matching.png";
            var contenttype = "image/png";
            storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = "receipt/" + filename, InternalFile = "budget-white-60x.png", ContentType = contenttype });

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
            _ = FakeObjects<Transaction>.Make(10).SaveTo(txrepo);

            // And: One receipt in storage, which will match MANY transactions
            var filename = $"Payee.png";
            var contenttype = "image/png";
            storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = "receipt/" + filename, InternalFile = "budget-white-60x.png", ContentType = contenttype });

            // When: Assigning the receipt to its best match
            var matched = await repository.AssignAll();

            // Then: No receipts were matched
            Assert.AreEqual(0, matched);

            // And: There is still just one (unassigned) receipt now
            var items = await repository.GetAllAsync();
            Assert.AreEqual(1, items.Count());
        }

        private void GivenMultipleReceipts(Transaction t)
        {
            // And: One receipt in storage, which will match the transaction we care about
            var filename = $"{t.Payee} ${t.Amount}.png";
            var contenttype = "image/png";
            storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = "receipt/" + filename, InternalFile = "budget-white-60x.png", ContentType = contenttype });

            // And: One receipt in storage, which will match MANY transactions
            filename = $"Payee.png";
            storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = "receipt/" + filename, InternalFile = "budget-white-60x.png", ContentType = contenttype });

            // And: One receipt in storage, which will NOT MATCH ANY transactions
            filename = $"Totally not matching.png";
            storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = "receipt/" + filename, InternalFile = "budget-white-60x.png", ContentType = contenttype });
        }

        [TestMethod]
        public async Task AssignAllVariousMatch()
        {
            // Given: Several transactions, one of which we care about
            var t = FakeObjects<Transaction>.Make(10).SaveTo(txrepo).Last();

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
            var contenttype = "image/png";
            storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = "receipt/" + filename, InternalFile = "budget-white-60x.png", ContentType = contenttype });

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
            await repository.DeleteAsync(new Receipt() { Filename = "receipt/" + filename });

            // Then: Fails silently
        }

        [TestMethod]
        public async Task GetMatchingOne()
        {
            // Given: Several transactions, one of which we care about
            var t = FakeObjects<Transaction>.Make(10).SaveTo(txrepo).Last();

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
    }
}
