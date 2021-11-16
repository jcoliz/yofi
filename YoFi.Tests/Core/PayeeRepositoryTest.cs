using Common.DotNet.Test;
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

namespace YoFi.Tests.Core
{
    [TestClass]
    public class PayeeRepositoryTest : BaseRepositoryTest<Payee>
    {
        protected override List<Payee> Items => new List<Payee>()
        {
                    new Payee() { ID = 1, Category = "B", Name = "3" },
                    new Payee() { ID = 2, Category = "A", Name = "2" },
                    new Payee() { ID = 3, Category = "C", Name = "5" },
                    new Payee() { ID = 4, Category = "A", Name = "1" },
                    new Payee() { ID = 5, Category = "B", Name = "4" },

                    new Payee() { Category = "ABCD", Name = "5" },
                    new Payee() { Category = "X", Name = "6" }
        };

        protected override int CompareKeys(Payee x, Payee y) => x.Name.CompareTo(y.Name);

        PayeeRepository payeeRepository => base.repository as PayeeRepository;

        [TestInitialize]
        public void SetUp()
        {
            context = new MockDataContext();
            repository = new PayeeRepository(context);
        }

        [TestMethod]
        public async Task BulkEdit()
        {
            // Given: Five items in the data set
            var all = Items.Take(5);

            // And: A subset of the items are selected
            var subset = all.Take(2);
            foreach (var item in subset)
                item.Selected = true;
            context.AddRange(all);

            // When: Applying bulk edit to the repository using a new category
            var newcategory = "New Category";
            await payeeRepository.BulkEditAsync(newcategory);

            // Then: The selected items have the new category
            Assert.IsTrue(subset.All(x => x.Category == newcategory));

            // And: The other items are unchanged
            Assert.IsTrue(all.Except(subset).All(x => x.Category != newcategory));

            // And: All items are unselected
            Assert.IsTrue(all.All(x => x.Selected != true));
        }

        [TestMethod]
        public async Task BulkEditNoChange()
        {
            // Given: Five items in the data set
            var all = Items.Take(5);

            // And: Taking a snapshot of that data for later comparison
            var expected = await DeepCopy.MakeDuplicateOf(all);

            // And: A subset of the items are selected
            var subset = all.Take(2);
            foreach (var item in subset)
                item.Selected = true;
            context.AddRange(all);

            // When: Applying bulk edit to the repository using null
            await payeeRepository.BulkEditAsync(null);

            // And: All items are unchanged
            Assert.IsTrue(expected.SequenceEqual(repository.All));

            // And: All items are unselected
            Assert.IsTrue(all.All(x => x.Selected != true));
        }

        [TestMethod]
        public async Task BulkDelete()
        {
            // Given: Five items in the data set
            var numtotalitems = 5;
            var all = Items.Take(numtotalitems);

            // And: A subset of the items are selected
            var numdeleteditems = 2;
            var subset = all.Take(numdeleteditems);
            foreach (var item in subset)
                item.Selected = true;
            context.AddRange(all);

            // When: Bulk deleting the selected items
            await payeeRepository.BulkDeleteAsync();

            // Then: The number of items is total minus deleted
            Assert.AreEqual(numtotalitems-numdeleteditems,repository.All.Count());

            // And: All repository items are unselected
            Assert.IsTrue(repository.All.All(x => x.Selected != true));
        }

        [TestMethod]
        public async Task NewFromTransaction()
        {
            // Given: A transaction in the DB
            const string payee = "Payee";
            const string category = "Category";
            var transaction = new Transaction() { ID = 1, Payee = payee, Category = category };
            context.Add(transaction);

            // When: Creating a new payee from that transaction
            var actual = await payeeRepository.NewFromTransactionAsync(transaction.ID);

            // Then: The resulting payee matches category & payee name
            Assert.AreEqual(payee, actual.Name);
            Assert.AreEqual(category, actual.Category);
        }

        [TestMethod]
        public virtual async Task Upload()
        {
            // Given: A spreadsheet with five items
            var expected = Items.Take(5);

            // When: Importing it via an importer
            await WhenImportingItemsAsSpreadsheet(expected);

            // Then: The expected items are in the dataset
            Assert.IsTrue(context.Get<Payee>().SequenceEqual(expected));
        }

        [TestMethod]
        public virtual async Task UploadAddNewDuplicate()
        {
            // Given: Five items in the data set
            var expected = Items.Take(5).ToList();
            context.AddRange(expected);

            // When: Uploading three new items, one of which the same as an already existing item
            // NOTE: These items are not EXACTLY duplicates, just duplicate enough to trigger the
            // hashset equality constraint on input.
            var upload = Items.Skip(5).Take(2).Concat(await DeepCopy.MakeDuplicateOf(expected.Take(1)));
            var actual = await WhenImportingItemsAsSpreadsheet(upload);

            // Then: Only two items were imported, because one exists
            Assert.AreEqual(2, actual.Count());

            // And: The data set now includes seven (not eight) items
            Assert.AreEqual(7, context.Get<Payee>().Count());
        }

        protected async Task<IEnumerable<Payee>> WhenImportingItemsAsSpreadsheet(IEnumerable<Payee> expected)
        {
            // Given: A spreadsheet with items as given
            using var stream = new MemoryStream();
            {
                using var ssw = new SpreadsheetWriter();
                ssw.Open(stream);
                ssw.Serialize(expected);
            }
            stream.Seek(0, SeekOrigin.Begin);

            // When: Importing it via an importer
            var importer = new BaseImporter<Payee>(repository);
            importer.QueueImportFromXlsx(stream);
            return await importer.ProcessImportAsync();
        }
    }
}
