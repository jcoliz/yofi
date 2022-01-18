using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Importers;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core
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

        public void Add<T>(IEnumerable<T> what, string name) where T : class
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
    public class UniversalImporterTest
    {
        public TestContext TestContext { get; set; }

        private UniversalImporter importer;
        private MockBudgetTxRepository budgetrepo;
        private MockPayeeRepository payeerepo;
        private MockTransactionRepository txrepo;

        static private IFormFile PrepareUpload(IEnumerable<object> what, string name)
        {
            // Build a spreadsheet with the chosen number of items
            // Note that we are not disposing the stream. User of the file will do so later.
            var stream = new MemoryStream();
            using (var ssr = new SpreadsheetWriter())
            {
                ssr.Open(stream);
                ssr.Serialize(what,name);
            }

            // Create a formfile with it
            stream.Seek(0, SeekOrigin.Begin);
            IFormFile file = new FormFile(stream, 0, stream.Length, name, $"{name}.xlsx");

            return file;
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
            var items = budgetrepo.MakeItems(5);
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
            var items = payeerepo.MakeItems(5);
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
            helper.Add(payeerepo.MakeItems(5),null);
            var items = budgetrepo.MakeItems(5);
            helper.Add(items, null);
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
            helper.Add(budgetrepo.MakeItems(5), null);
            var items = payeerepo.MakeItems(5);
            helper.Add(items, null);
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
            helper.Add(budgetrepo.MakeItems(5), null);
            helper.Add(payeerepo.MakeItems(5), null);
            var items = txrepo.MakeItems(5);
            helper.Add(items, null);
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
            var items = txrepo.MakeItems(5);
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
            var items = txrepo.MakeItems(5);
            helper.Add(items, name);
            var splits = new List<Split>()
            {
                new Split() { Amount = 100m, Category = "1", TransactionID = 1 },
                new Split() { Amount = 200m, Category = "2A", TransactionID = 2 },
                new Split() { Amount = 200m, Category = "2B", TransactionID = 2 },
                new Split() { Amount = 300m, Category = "3A", TransactionID = 3 },
                new Split() { Amount = 300m, Category = "3B", TransactionID = 3 },
                new Split() { Amount = 300m, Category = "3B", TransactionID = 3 }
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
                var tx = txrepo.Items.Where(x => x.Payee == i.ToString()).Single();
                Assert.AreEqual(i, tx.Splits.Count);
            }
            while (--i > 0);

        }

        [TestMethod]
        public async Task All()
        {
            // Given: A spreadsheet with ALL kinds of data in a single sheet
            using var helper = new ImportPackageHelper();
            var bitems = budgetrepo.MakeItems(4);
            helper.Add(bitems, null);
            var pitems = payeerepo.MakeItems(5);
            helper.Add(pitems, null);
            var txitems = txrepo.MakeItems(6);
            helper.Add(txitems, null);
            var splits = new List<Split>()
            {
                new Split() { Amount = 100m, Category = "1", TransactionID = 1 },
                new Split() { Amount = 200m, Category = "2A", TransactionID = 2 },
                new Split() { Amount = 200m, Category = "2B", TransactionID = 2 },
                new Split() { Amount = 300m, Category = "3A", TransactionID = 3 },
                new Split() { Amount = 300m, Category = "3B", TransactionID = 3 },
                new Split() { Amount = 300m, Category = "3B", TransactionID = 3 }
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
            Assert.IsTrue(splits.SequenceEqual(txrepo.Items.SelectMany(x=>x.Splits ?? Enumerable.Empty<Split>())));
        }

        public void TransactionsOfx()
        {
            // Given: An OFX file with transactions
            // When: Importing it
            // Then: All items imported
        }
    }
}
