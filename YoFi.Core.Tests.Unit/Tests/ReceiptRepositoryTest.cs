using Common.DotNet;
using Common.DotNet.Test;
using DocumentFormat.OpenXml.Office2010.Excel;
using jcoliz.FakeObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoFi.Core;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Tests.Helpers;

namespace YoFi.Core.Tests.Unit
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
        IDataProvider context;

        #endregion

        [TestInitialize]
        public void SetUp()
        {
            storage = new TestAzureStorage();
            txrepo = new MockTransactionRepository() { Storage = storage };
            
            // Match clock with fakeobjectsmaker            
            clock = new TestClock() { Now = new DateTime(2001, 12, 31) };

            context = new MockDataContext();
            repository = new ReceiptRepositoryInDb(context, txrepo, storage, clock);
        }

        #region Helpers

        public void AddRange(System.Collections.IEnumerable objects)
        {
            if (objects is IEnumerable<Transaction> txs)
            {
                txrepo.AddRangeAsync(txs).Wait();
            }
            else if (objects is IEnumerable<Receipt> r)
            {
                context.AddRange(r);
            }
            else
                throw new NotImplementedException();
        }
        private Receipt GivenReceiptInStorage(string filename)
        {
            var item = Receipt.FromFilename(filename, clock: clock);
            context.Add(item);
            context.SaveChangesAsync().Wait();
            storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = $"{ReceiptRepositoryInDb.Prefix}{item.ID}", InternalFile = "budget-white-60x.png", ContentType = contenttype });

            return item;
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
            Assert.AreEqual($"{ReceiptRepositoryInDb.Prefix}{items.Single().ID}", storage.BlobItems.Single().FileName);
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
        public async Task GetOneTransactionViaGetById()
        {
            // Given: One transaction
            var tx = FakeObjects<Transaction>.Make(1).SaveTo(this).Single();

            // And: One receipt in storage which will match that
            var filename = $"{tx.Payee} {tx.Timestamp.ToString("MM-dd")}.png";
            var r = GivenReceiptInStorage(filename);

            // When: Getting The particular receipt
            var actual = await repository.GetByIdAsync(r.ID);

            // Then: The transaction is listed among the matches
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
            // Given: A transaction
            var t = FakeObjects<Transaction>.Make(1).SaveTo(this).Single();

            // And: One receipt in storage which matches
            var filename = $"{t.Payee} ${t.Amount} {t.Timestamp.Month}-{t.Timestamp.Day}.png";
            var r = GivenReceiptInStorage(filename);

            // When: Assigning the receipt to the transaction           
            await repository.AssignReceipt(r, t);

            // Then: The transaction displays as having a receipt
            Assert.IsFalse(string.IsNullOrEmpty(t.ReceiptUrl));

            // And: The receipt is contained in storage as expected
            var blob = storage.BlobItems.Where(x => x.FileName == ReceiptRepositoryInDb.Prefix + r.ID.ToString()).Single();
            Assert.AreEqual(contenttype,blob.ContentType);

            // And: There are no more (unassigned) receipts now
            var items = await repository.GetAllAsync();
            Assert.IsFalse(items.Any());
        }

        // Bug 1554: [Production Bug]: 400 Bad Request when matching receipts from Edit
        //
        // It should be supported to match a receipt that doesn't actually have ANY matching
        // attributes.
#if false
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]  
        public async Task AssignReceiptNoMatch()
        {
            // Given: A transaction
            var t = FakeObjects<Transaction>.Make(1).SaveTo(this).Single();

            // And: One receipt in storage which will NOT match
            var filename = $"Bogus ${t.Amount * 2} .png";
            var r = GivenReceiptInStorage(filename);

            // When: Assigning the receipt to the transaction           
            await repository.AssignReceipt(r, t);

            // Then: Throws exception
        }
#endif

        [TestMethod]
        public async Task AssignReceiptWithMemo()
        {
            // Given: A transaction
            var t = FakeObjects<Transaction>.Make(1).SaveTo(this).Single();

            // And: One receipt in storage which matches, and contains a memo
            var newmemo = "This is a whole new memo!!";
            var filename = $"{t.Payee} ${t.Amount} {t.Timestamp.Month}-{t.Timestamp.Day} {newmemo}.png";
            var r = GivenReceiptInStorage(filename);

            // When: Assigning the receipt to the transaction           
            await repository.AssignReceipt(r, t);

            // Then: The transaction contians the memo from the receipt
            Assert.AreEqual(newmemo,t.Memo);
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
            var blob = storage.BlobItems.Where(x => x.FileName == $"{ReceiptRepositoryInDb.Prefix}1").Single();
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
            var r = GivenReceiptInStorage(filename);

            // When: Deleting it
            await repository.DeleteAsync(r);

            // And: Getting All
            var items = await repository.GetAllAsync();

            // Then: Nothing returned
            Assert.IsFalse(items.Any());
        }

        [TestMethod]
        public async Task DeleteFromNone()
        {
            // Given: No receipts

            // When: Deleting a nonexistent receipt
            await repository.DeleteAsync(new Receipt() { ID = 100 });

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
            Assert.AreEqual(2,matches.Matches);

            // And: The first one is the best one
            var best = matches.Suggested;
            Assert.AreEqual(t.Payee, best.Name);
        }

        [TestMethod]
        public async Task GetMatchingButNotAmount()
        {
            // Given: Several transactions, one of which we care about
            var t = FakeObjects<Transaction>.Make(10).SaveTo(this).Skip(5).First();

            // And: One receipt which matches the transaction BUT has the wrong amount
            var filename = $"{t.Payee} ${t.Amount+2m} {t.Timestamp.Month}-{t.Timestamp.Day}.png";
            GivenReceiptInStorage(filename);

            // When: Querying All
            var items = await repository.GetAllAsync();

            // Then: The best match for the receipt is the given transaction
            Assert.AreEqual(t, items.Single().Matches.First());
        }

        [TestMethod]
        public async Task GetMatchingByDateOnly()
        {
            // Given: Several transactions, one of which we care about
            var t = FakeObjects<Transaction>.Make(10).SaveTo(this).Skip(5).First();

            // And: One receipt which matches the transaction by date and otherwise matches ALL transactions
            var filename = $"Payee {t.Timestamp.Month}-{t.Timestamp.Day}.png";
            GivenReceiptInStorage(filename);

            // When: Querying All
            var items = await repository.GetAllAsync();

            // Then: The best match for the receipt is the given transaction
            Assert.AreEqual(t, items.Single().Matches.First());
        }

        [TestMethod]
        public async Task GetOrderByMatching()
        {
            // Given: Several transactions, one of which we care about
            var t = FakeObjects<Transaction>.Make(10).SaveTo(this).Last();

            // And: Multiple receipts, one matches this transaction, one matches all, one matches none
            GivenMultipleReceipts(t);

            // When: Asking for the receipt(s) ordered by matching THIS transaction
            var rs = await repository.GetAllOrderByMatchAsync(t);

            // Then: There all receipts are returned
            Assert.AreEqual(3,rs.Count());

            // And: The first one is the best one
            var best = rs.First();
            Assert.AreEqual(t.Payee, best.Name);

            // And: The last one matches not at all
            var nomatch = rs.Last();
            var quality = nomatch.MatchesTransaction(t);
            Assert.AreEqual(0,quality);
        }


        [TestMethod]
        public async Task EndToEnd()
        {
            // Given: A real tx repository, and a receipt repository build off that
            var mockdc = new MockDataContext();
            txrepo = new TransactionRepository(mockdc,clock,null,null,storage);
            repository = new ReceiptRepositoryInDb(context, txrepo, storage, clock);

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

            // And: The receipt name identifies a receipt which is no longer in the system
            // (because we deleted it)
            var namematch = new Regex($"^{ReceiptRepositoryInDb.Prefix}(?<id>[0-9]+)$");
            var match = namematch.Match(name);
            Assert.IsTrue(match.Success);

            var id = int.Parse(match.Groups["id"].Value);
            Assert.IsFalse((await repository.GetAllAsync()).Where(x=>x.ID == id).Any());
        }

        [TestMethod]
        public async Task AcceptOne()
        {
            // Given: Several transactions, one of which we care about
            // Note: We have to override the timestamp on these to match the clock
            // that the system under test is using, else the transaction wont match the receipt
            // because the years will be off.
            var i = 0;
            var t = FakeObjects<Transaction>.Make(10, x => x.Timestamp = DateTime.Now - TimeSpan.FromDays(i++)).SaveTo(this).Last();

            // And: One receipt in storage, which will match the transaction we care about
            var filename = $"{t.Payee} ${t.Amount} {t.Timestamp.Month}-{t.Timestamp.Day}.png";
            var r = GivenReceiptInStorage(filename);

            // When: Assigning the receipt to its best match
            await repository.AssignReceipt(r.ID, t.ID);

            // Then: The selected transaction has a receipt now, with the expected name
            var expected = $"{ReceiptRepositoryInDb.Prefix}{r.ID}";
            var actual = txrepo.All.Where(x => x.ID == t.ID).Single();
            Assert.AreEqual(expected, actual.ReceiptUrl);

            // And: There are no more (unassigned) receipts now
            Assert.IsFalse((await repository.GetAllAsync()).Any());
        }


        [TestMethod]
        public async Task Bug1348()
        {
            //
            // Bug 1348: [Production Bug] Receipt shows matches on index but not on details
            //

            /*
                Create transaction exactly 15 days prior to the current date
                Create two receipts, both which match the transaction by name, and not by amount. One receipt is on the current date. The other receipt is a week prior.
                View the receipts on the receipt index.
                Note that both receipts show matches > 0, so "create" is not shown, but "review" is shown.
                Expected: Current transaction should have 0 matches, and create is shown
                Click review on the current-date transaction
                Note that the details page shows no matches
                Expected: This is correct
            */

            // Given: Transaction exactly 15 days prior
            var today = DateTime.Now.Date;
            var prior = today - TimeSpan.FromDays(15);
            var txs = FakeObjects<Transaction>.Make(1, x => x.Timestamp = prior).SaveTo(this);
            var tx = txs.Single();

            // And: One receipt matches by name, not amount, on today's date
            // And: One receipt matches by name, not amount, on an earlier date
            var r = FakeObjects<Receipt>
                        .Make(1, x => { x.Timestamp = today; x.Amount = tx.Amount * 2; x.Name = tx.Payee; })
                        .Add(1, x => { x.Timestamp = today - TimeSpan.FromDays(7); x.Amount = tx.Amount * 2; x.Name = tx.Payee; })
                        .SaveTo(this)
                        .First();

            // When: Getting All
            var items = await repository.GetAllAsync();

            // Then: First item shows no matches
            var nmatches = items.First().Matches.Count;
            Assert.AreEqual(0, nmatches);

            // When: Getting details for the first receipt
            var details = await repository.GetByIdAsync(r.ID);

            // Then: Item shows no matches
            Assert.AreEqual(0, details.Matches.Count);
        }


        [TestMethod]
        public async Task Bug1351()
        {
            //
            // Bug 1351: Should be able to create a new transaction for a receipt which matches another
            //

            /*
                Given: A transaction
                And: Two receipts, both which match the transaction, but one of them matches better than the other
                When: Getting the receipts index
                Then: Both receipts have a "create" button

                NOTE: Looking back, I don't see any logic for why they WOULDN'T have a create button. 
                As long as the receipts are in the list, they would have a create button

                So, in porting this test, I'll just make sure they both exists.
            */

            // Given: A transaction
            var t = FakeObjects<Transaction>.Make(1).SaveTo(this).Single();

            // And: Two receipts, both which match the transaction, but one of them matches better than the other
            int nextid = 1;
            var rs = FakeObjects<Receipt>
                .Make(1, x => { x.ID = nextid++; x.Name = t.Payee; x.Amount = t.Amount; x.Timestamp = t.Timestamp; })
                .Add(1, x => { x.ID = nextid++; x.Name = t.Payee; x.Amount = t.Amount * 2; x.Timestamp = t.Timestamp; })
                .SaveTo(this);

            // When: Getting All
            var items = await repository.GetAllAsync();

            // Then: Both receipts are returned
            Assert.IsTrue(items.Any(x => x.ID == 1));
            Assert.IsTrue(items.Any(x => x.ID == 2));
        }


        [TestMethod]
        public async Task AcceptPick()
        {
            // Given: Several transactions, one of which we care about
            // Note: We have to override the timestamp on these to match the clock
            // that the system under test is using, else the transaction wont match the receipt
            // because the years will be off.
            var i = 0;
            var t = FakeObjects<Transaction>.Make(10, x => x.Timestamp = DateTime.Now - TimeSpan.FromDays(i++)).SaveTo(this).Last();

            // And: One receipt in storage, which will match the transaction we care about
            var filename = $"{t.Payee} ${t.Amount} {t.Timestamp.Month}-{t.Timestamp.Day}.png";
            var r = GivenReceiptInStorage(filename);

            // When: Assigning the receipt to its best match
            // And: Asking for the redirect (next) to edit transaction
            await repository.AssignReceipt(r.ID, t.ID);

            // Then: The selected transaction has a receipt now, with the expected name
            var expected = $"{ReceiptRepositoryInDb.Prefix}{r.ID}";
            var actual = txrepo.All.Where(x => x.ID == t.ID).Single();
            Assert.AreEqual(expected, actual.ReceiptUrl);

            // And: There are no more (unassigned) receipts now
            Assert.IsFalse((await repository.GetAllAsync()).Any());
        }

        [TestMethod]
        public async Task PickAll()
        {
            // Given: A transaction in the database
            var tx = FakeObjects<Transaction>.Make(1).SaveTo(this).Single();

            // And: Many receipts in the database all of which match that transaction
            var rs = FakeObjects<Receipt>
                .Make(5, x => { x.Name = tx.Payee; x.Timestamp = tx.Timestamp; })
                .SaveTo(this);

            // When: Getting the receipt picker for a the transaction
            var qresult = await repository.GetAllOrderByMatchAsync(tx.ID);

            // Then: All receipts are included in the results
            CollectionAssert.AreEquivalent(qresult.Select(x => x.Memo).ToArray(), rs.Select(x => x.Memo).ToArray());
        }

        [TestMethod]
        public async Task PickSome()
        {
            // Given: A transaction in the database
            var tx = FakeObjects<Transaction>.Make(1).SaveTo(this).Single();

            // And: Many receipts in the database some of which match that transaction,
            // but others which do not match
            var rs = FakeObjects<Receipt>
                .Make(5, x => { x.Name = tx.Payee; x.Timestamp = tx.Timestamp; })
                .Add(7, x => { x.Name = "No Match"; x.Amount = tx.Amount * 10; })
                .SaveTo(this);

            // When: Getting the receipt picker for a the transaction
            var qresult = await repository.GetAllOrderByMatchAsync(tx.ID);

            // Then: All receipts are included in the results
            CollectionAssert.AreEquivalent(qresult.Select(x => x.Memo).ToArray(), rs.Select(x => x.Memo).ToArray());
        }

        [TestMethod]
        public async Task PickNone()
        {
            // Given: A transaction in the database
            var tx = FakeObjects<Transaction>.Make(1).SaveTo(this).Single();

            // And: Many receipts in the database none of which match that transaction
            var rs = FakeObjects<Receipt>
                .Make(5, x => { x.Name = "No Match"; x.Amount = 0; x.Timestamp += TimeSpan.FromDays(100); })
                .SaveTo(this);

            // When: Getting the receipt picker for a the transaction
            var qresult = await repository.GetAllOrderByMatchAsync(tx.ID);

            // Then: All receipts are included in the results
            CollectionAssert.AreEquivalent(qresult.Select(x => x.Memo).ToArray(), rs.Select(x => x.Memo).ToArray());
        }

        #endregion
    }
}
