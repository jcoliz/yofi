using jcoliz.FakeObjects;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Importers;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Tests.Helpers;

namespace YoFi.Core.Tests.Unit
{
    internal class ImportPackageHelper : IDisposable
    {
        private readonly MemoryStream _stream;
        private readonly SpreadsheetWriter _writer;

        public ImportPackageHelper()
        {
            _stream = new MemoryStream();
            _writer = new SpreadsheetWriter();

            _writer.Open(_stream);
        }

        public void Add<T>(IEnumerable<T> what, string name = null) where T : class
        {
            _writer.Serialize(what, name);
        }

        public Stream GetFile(TestContext testContext)
        {
            _writer.Dispose();

            _stream.Seek(0, SeekOrigin.Begin);

            var dir = testContext.FullyQualifiedTestClassName;
            Directory.CreateDirectory(dir);
            var filename = $"{dir}/{testContext.TestName}.xlsx";
            File.Delete(filename);
            using var outstream = File.OpenWrite(filename);
            _stream.CopyTo(outstream);
            testContext.AddResultFile(filename);

            return _stream;
        }

        public void Dispose()
        {
            _stream.Dispose();
        }
    }

    [TestClass]
    public class UniversalImporterTest: IFakeObjectsSaveTarget
    {
        #region Properties

        public TestContext TestContext { get; set; }

        #endregion

        #region Fields

        private UniversalImporter importer;
        private MockBudgetTxRepository budgetrepo;
        private MockPayeeRepository payeerepo;
        private MockTransactionRepository txrepo;

        #endregion

        #region Helpers

        public void AddRange(System.Collections.IEnumerable objects)
        {
            if (objects is IEnumerable<Transaction> txs)
            {
                txrepo.AddRangeAsync(txs).Wait();
            }
            else
                throw new NotImplementedException();
        }

        private MemoryStream PrepareSpreadsheet<T>(IEnumerable<T> what, string name) where T: class
        {
            var stream = new MemoryStream();
            using (var ssr = new SpreadsheetWriter())
            {
                ssr.Open(stream);
                ssr.Serialize(what, name);
            }
            stream.Seek(0, SeekOrigin.Begin);

            var dir = TestContext.FullyQualifiedTestClassName;
            Directory.CreateDirectory(dir);
            var filename = $"{dir}/{TestContext.TestName}.xlsx";
            File.Delete(filename);
            using var outstream = File.OpenWrite(filename);
            stream.CopyTo(outstream);
            TestContext.AddResultFile(filename);

            return stream;
        }

        #endregion

        [TestInitialize]
        public void SetUp()
        {
            budgetrepo = new MockBudgetTxRepository();
            payeerepo = new MockPayeeRepository();
            txrepo = new MockTransactionRepository();
            importer = new UniversalImporter(new AllRepositories(txrepo, budgetrepo, payeerepo));
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(importer);
        }

        /// <summary>
        /// Tests the case where we DO know to expect budget TX items.
        /// </summary>
        /// <remarks>
        /// In this case, it's irrelevant what the sheets are named
        /// </remarks>
        /// <returns></returns>
        [TestMethod]
        public async Task BudgetTxNoName()
        {
            // Given: A spreadsheet with budget transactions in a sheet named {Something Random}
            var items = FakeObjects<BudgetTx>.Make(5);
            using var stream = PrepareSpreadsheet(items, "Random-Budget-Tx");

            // When: Importing it
            importer.QueueImportFromXlsx<BudgetTx>(stream);
            await importer.ProcessImportAsync();

            // Then: All items imported
            Assert.IsTrue(items.SequenceEqual(budgetrepo.Items));
        }

        [TestMethod]
        public async Task PayeeNoName()
        {
            // Given: A spreadsheet with payees in a sheet named {Something Random}
            var items = FakeObjects<Payee>.Make(5);
            using var stream = PrepareSpreadsheet(items, "Random-Payee-s");

            // When: Importing it
            importer.QueueImportFromXlsx<Payee>(stream);
            await importer.ProcessImportAsync();

            // Then: All items imported
            Assert.IsTrue(items.SequenceEqual(payeerepo.Items));
        }

        [TestMethod]
        public async Task BudgetTx()
        {
            // Given: A spreadsheet with budget transactions in a sheet named "BudgetTx", and other kinds of data too
            using var helper = new ImportPackageHelper();
            helper.Add(FakeObjects<Payee>.Make(5));
            var items = FakeObjects<BudgetTx>.Make(5);
            helper.Add(items);
            var stream = helper.GetFile(TestContext);

            // When: Importing it
            importer.QueueImportFromXlsx(stream);
            await importer.ProcessImportAsync();

            // Then: All items imported
            Assert.IsTrue(items.SequenceEqual(budgetrepo.Items));
        }

        [TestMethod]
        public async Task Payee()
        {
            // Given: A spreadsheet with payees in a sheet named "Payee", and all other kinds of data too
            using var helper = new ImportPackageHelper();
            helper.Add(FakeObjects<BudgetTx>.Make(5));
            var items = FakeObjects<Payee>.Make(5);
            helper.Add(items);
            var stream = helper.GetFile(TestContext);

            // When: Importing it
            importer.QueueImportFromXlsx(stream);
            await importer.ProcessImportAsync();

            // Then: All items imported
            Assert.IsTrue(items.SequenceEqual(payeerepo.Items));
        }

        [TestMethod]
        public async Task Transactions()
        {
            // Given: A spreadsheet with transactions in a sheet named "Transaction", and all other kinds of data too
            using var helper = new ImportPackageHelper();
            helper.Add(FakeObjects<BudgetTx>.Make(5));
            helper.Add(FakeObjects<Payee>.Make(5));
            var items = FakeObjects<Transaction>.Make(5);
            helper.Add(items);
            var stream = helper.GetFile(TestContext);

            // When: Importing it
            importer.QueueImportFromXlsx(stream);
            await importer.ProcessImportAsync();

            // Then: All items imported
            Assert.IsTrue(items.SequenceEqual(txrepo.Items));
        }

        [TestMethod]
        public async Task TransactionsNoName()
        {
            // Given: A spreadsheet with transactions in a sheet named "Transaction", and all other kinds of data too
            using var helper = new ImportPackageHelper();
            var items = FakeObjects<Transaction>.Make(5);
            helper.Add(items, "Random-Tranxs");
            var stream = helper.GetFile(TestContext);

            // When: Importing it
            importer.QueueImportFromXlsx(stream);
            await importer.ProcessImportAsync();

            // Then: All items imported
            Assert.IsTrue(items.SequenceEqual(txrepo.Items));
        }

        [DataRow("Random-Tranxs")]
        [DataRow(null)]
        [DataTestMethod]
        public async Task TransactionsAndSplits(string name)
        {
            // Given: A spreadsheet with transactions in sheet named {Something Random} and splits in "Split"
            using var helper = new ImportPackageHelper();
            var items = FakeObjects<Transaction>.Make(5, x => x.ID = (int)(x.Amount / 100m)).Group(0);
            helper.Add(items, name);
            var splits = new List<Split>()
            {
                new Split() { Amount = 100m, Category = "1", TransactionID = items[0].ID },
                new Split() { Amount = 200m, Category = "2A", TransactionID = items[1].ID },
                new Split() { Amount = 200m, Category = "2B", TransactionID = items[1].ID },
                new Split() { Amount = 300m, Category = "3A", TransactionID = items[2].ID },
                new Split() { Amount = 300m, Category = "3B", TransactionID = items[2].ID },
                new Split() { Amount = 300m, Category = "3B", TransactionID = items[2].ID }
            };
            helper.Add(splits, null);
            var stream = helper.GetFile(TestContext);

            // When: Importing it
            importer.QueueImportFromXlsx(stream);
            await importer.ProcessImportAsync();

            // Then: All items imported
            Assert.IsTrue(items.SequenceEqual(txrepo.Items));

            int i = 3;
            do
            {
                var tx = txrepo.Items.Where(x => x.Amount == i * 100m).Single();
                Assert.AreEqual(i, tx.Splits.Count);
            }
            while (--i > 0);

        }

        [TestMethod]
        public async Task All()
        {
            // Given: A spreadsheet with ALL kinds of data in a single sheet
            using var helper = new ImportPackageHelper();
            var bitems = FakeObjects<BudgetTx>.Make(4);
            helper.Add(bitems, null);
            var pitems = FakeObjects<Payee>.Make(5);
            helper.Add(pitems, null);
            var txitems = FakeObjects<Transaction>.Make(6,x=>x.ID = (int)(x.Amount / 100m)).Group(0);
            helper.Add(txitems, null);
            var splits = new List<Split>()
            {
                new Split() { Amount = 100m, Category = "1", TransactionID = txitems[0].ID },
                new Split() { Amount = 200m, Category = "2A", TransactionID = txitems[1].ID },
                new Split() { Amount = 200m, Category = "2B", TransactionID = txitems[1].ID },
                new Split() { Amount = 300m, Category = "3A", TransactionID = txitems[2].ID },
                new Split() { Amount = 300m, Category = "3B", TransactionID = txitems[2].ID },
                new Split() { Amount = 300m, Category = "3B", TransactionID = txitems[3].ID }
            };
            helper.Add(splits, null);
            var stream = helper.GetFile(TestContext);

            // When: Importing it
            importer.QueueImportFromXlsx(stream);
            await importer.ProcessImportAsync();

            // Then: All items imported
            Assert.IsTrue(txitems.SequenceEqual(txrepo.Items));
            Assert.IsTrue(pitems.SequenceEqual(payeerepo.Items));
            Assert.IsTrue(bitems.SequenceEqual(budgetrepo.Items));

            var reposplits = txrepo.Items.Where(x => x.HasSplits).SelectMany(x => x.Splits);
            Assert.IsTrue(splits.SequenceEqual(reposplits));

            Assert.AreEqual(1, txrepo.Items[0].Splits.Count);
            Assert.AreEqual(2, txrepo.Items[1].Splits.Count);
            Assert.AreEqual(2, txrepo.Items[2].Splits.Count);
            Assert.AreEqual(1, txrepo.Items[3].Splits.Count);
        }

        [TestMethod]
        public async Task TransactionsOfx()
        {
            // Given: An OFX file with transactions
            var stream = Common.DotNet.Test.SampleData.Open("SampleData-2022-Full-Month02.ofx");

            // When: Importing it
            await importer.QueueImportFromOfxAsync(stream);
            await importer.ProcessImportAsync();

            // Then: All items imported
            Assert.AreEqual(73, txrepo.Items.Count);

            // And: Spot check facts we know about the items
            Assert.IsTrue(txrepo.Items.All(x => x.Timestamp.Month == 2));
        }

        [TestMethod]
        public async Task UploadHighlights()
        {
            // Given: Two chunks of transactions
            var original = FakeObjects<Transaction>.Make(10);

            // And: First chunk has assigned bankrefs
            // And: Having made subtle changes to the transactions AFTER generating the bankref
            foreach (var item in original.Group(0))
            {
                item.GenerateBankReference();
                item.Timestamp -= TimeSpan.FromDays(10);
            }

            // And: Is in the DB
            await txrepo.AddRangeAsync(original.Group(0));

            // And: A spreadsheet of all the items, which overlaps the originally-uploaded items
            var upload = FakeObjects<Transaction>.Make(15);
            using var helper = new ImportPackageHelper();
            helper.Add(upload);
            var stream = helper.GetFile(TestContext);

            // When: Importing it
            importer.QueueImportFromXlsx(stream);
            await importer.ProcessImportAsync();

            // Then: Only the non-overlapping items are selected
            var selected = txrepo.All.Where(x => x.Selected == true).OrderBy(TestKey<Transaction>.Order()).ToList();
            Assert.IsTrue(selected.SequenceEqual(upload.Skip(10)));

            // And: The overlapping new transactions are highlighted and deselected, indicating that they
            // are probably duplicates
            var highlights = importer.HighlightIDs.ToHashSet();
            var highlighted = txrepo.All.Where(x => highlights.Contains(x.ID)).ToList();
            var expected = upload.Take(10);
            Assert.IsTrue(expected.SequenceEqual(highlighted));
        }

        [TestMethod]
        public async Task UploadCreateSplits()
        {
            // Given: One transaction which matches the special hard-coded payee, and has no category (which will allow payee matching)
            var items = FakeObjects<Transaction>.Make(1, x => { x.Payee = "NameMatch"; x.Category = null; });
            using var helper = new ImportPackageHelper();
            helper.Add(items);
            var stream = helper.GetFile(TestContext);

            // When: Importing it
            importer.QueueImportFromXlsx(stream);
            await importer.ProcessImportAsync();

            // Then: Splits were generated for the item
            Assert.AreEqual(3, txrepo.All.Single().Splits.Count());
        }

        [TestMethod]
        public async Task UploadSplitsForTransaction()
        {
            // Given: There are 5 items in the database, one of which we care about
            var expected = FakeObjects<Transaction>.Make(5).SaveTo(this).Last();
            var id = expected.ID;

            // And: A spreadsheet of splits which will break down the details of that transaction
            var numsplits = 4;
            var splits = FakeObjects<Split>.Make(numsplits, x => x.Amount = expected.Amount / numsplits ).OrderBy(TestKey<Split>.Order());
            using var helper = new ImportPackageHelper();
            helper.Add(splits);
            var stream = helper.GetFile(TestContext);

            // And: A split importer targeting this txrepo
            var splitimporter = new SplitImporter(txrepo);

            // When: Uploading the spreadsheet for the transaction
            splitimporter.Target = expected;
            splitimporter.QueueImportFromXlsx(stream);
            await splitimporter.ProcessImportAsync();

            // Then : The splits are now attached to the transaction in the database
            var actual = txrepo.All.Where(x => x.ID == id).Single();
            Assert.IsTrue(actual.HasSplits);
            Assert.IsTrue(actual.IsSplitsOK);
            Assert.IsTrue(splits.SequenceEqual(actual.Splits.OrderBy(TestKey<Split>.Order())));
        }


    }
}
