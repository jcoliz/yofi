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

        public IFormFile GetFile(string filename)
        {
            _writer.Dispose();

            _stream.Seek(0, SeekOrigin.Begin);
            return new FormFile(_stream, 0, _stream.Length, filename, $"{filename}.xlsx");
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
            var budgetimport = new BaseImporter<BudgetTx>(budgetrepo);
            payeerepo = new MockPayeeRepository();
            var payeeimport = new BaseImporter<Payee>(payeerepo);
            var txrepo = new MockTransactionRepository();
            var tximport = new TransactionImporter(txrepo, payeerepo);
            importer = new UniversalImporter(tximport, payeeimport, budgetimport);
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
            items.SequenceEqual(budgetrepo.Items);
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
            items.SequenceEqual(payeerepo.Items);
        }
        public void BudgetTx()
        {
            // Given: A spreadsheet with budget transactions in a sheet named "BudgetTx", and all other kinds of data too
            // When: Importing it
            // Then: All items imported
        }
        public void Payee()
        {
            // Given: A spreadsheet with payees in a sheet named "Payee", and all other kinds of data too
            // When: Importing it
            // Then: All items imported
        }
        public void Transactions()
        {
            // Given: A spreadsheet with transactions in a sheet named "Transaction", and all other kinds of data too
            // When: Importing it
            // Then: All items imported
        }
        public void TransactionsNoName()
        {
            // Given: A spreadsheet with transactions in a sheet named {Something Random} and splits in a sheet named {something random}
            // When: Importing it
            // Then: All items imported
        }
        public void TransactionsAndSplitsNoName()
        {
            // Given: A spreadsheet with transactions and splits in sheets each named {Something Random}
            // When: Importing it
            // Then: All items imported
        }
        public void TransactionsAndSplits()
        {
            // Given: A spreadsheet with transactions and splits in sheets each named "Transaction", and "Split"
            // When: Importing it
            // Then: All items imported
        }
        public void TransactionsOfx()
        {
            // Given: An OFX file with transactions
            // When: Importing it
            // Then: All items imported
        }
    }
}
