using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.AspNet.Core.Repositories;
using YoFi.AspNet.Models;
using YoFi.Core.Importers;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class BudgetTxRepositoryTest
    {
        private IRepository<BudgetTx> repository;
        private MockDataContext context;

        public static List<BudgetTx> Items
        {
            get
            {
                return new List<BudgetTx>()
                {
                    new BudgetTx() { ID = 1, Timestamp = new System.DateTime(2020, 06, 01),  Category = "A", Amount = 100m },
                    new BudgetTx() { ID = 2, Timestamp = new System.DateTime(2020, 06, 01),  Category = "B", Amount = 200m },
                    new BudgetTx() { ID = 3, Timestamp = new System.DateTime(2020, 05, 01),  Category = "C", Amount = 500m },
                    new BudgetTx() { ID = 4, Timestamp = new System.DateTime(2020, 05, 01),  Category = "B", Amount = 400m },
                    new BudgetTx() { ID = 5, Timestamp = new System.DateTime(2020, 05, 01),  Category = "A", Amount = 300m },
                    new BudgetTx() { Timestamp = new System.DateTime(2020, 06, 01), Category = "C", Amount = 700m },
                    new BudgetTx() { Timestamp = new System.DateTime(2020, 07, 01), Category = "A", Amount = 800m },
                };
            }
        }

        /// <summary>
        /// Compares two items based on their test sort key, returning a comparison
        ///     of their relative values.
        /// </summary>
        /// <remarks>
        /// Test keys are an intern notation, a way for us to independently indicate our expectations for how
        /// the data should be correctly sorted. This is a choice we made when creating our sample data,
        /// so can't be inferred from the structure of the class.
        /// </remarks>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>
        ///     A signed number indicating the relative values of the two items. Return
        ///     value Meaning Less than zero <paramref name="x"/>is less than <paramref name="y"/>. 
        ///     Zero the instances are equal.
        private int CompareKeys(BudgetTx x, BudgetTx y) => x.Amount.CompareTo(y.Amount);

        [TestInitialize]
        public void SetUp()
        {
            context = new MockDataContext();
            repository = new BudgetTxRepository(context);
        }

        [TestMethod]
        public void Empty()
        {
            // When: Creating a new repository (Done in setup)

            // Then: Repository created successfully
            Assert.IsNotNull(repository);
        }

        [TestMethod]
        public void IndexEmpty()
        {
            // Given: No items in the repository

            // When: Querying items from the repository
            var model = repository.ForQuery(null);

            // Then: No items returned
            Assert.AreEqual(0, model.Count());
        }

        [TestMethod]

        public void IndexSingle()
        {
            // Given: A single item in the data set
            var expected = Items.Take(1);
            context.AddRange(expected);

            // When: Querying items from the repository
            var model = repository.ForQuery(null);

            // Then: The results match what we added to the data set
            Assert.IsTrue(model.SequenceEqual(expected));
        }

        [TestMethod]
        public void IndexMany()
        {
            var expected = Items.Take(5).ToList();
            context.AddRange(expected);

            var model = repository.ForQuery(null);

            // Test that the resulting items are the same as expected items ordered correctly
            expected.Sort(CompareKeys);
            Assert.IsTrue(expected.SequenceEqual(model));
        }

        [TestMethod]
        public void IndexQ()
        {
            // Given: Five items in the data set
            var all = Items.Take(5);
            context.AddRange(all);

            // When: Querying items from the repository for a single category
            var lookup = all.ToLookup(x => x.Category, x=>x);
            var expected = lookup.First();
            var category = expected.Key;
            var model = repository.ForQuery(category);

            // Then: Only the matching items are returned
            Assert.IsTrue(expected.SequenceEqual(model));
        }

        [TestMethod]
        public async Task DetailsFound()
        {
            // Given: Five items in the data set
            context.AddRange(Items.Take(5));

            // When: Getting a single item by its ID
            var expected = Items.Skip(3).First();
            var model = await repository.GetByIdAsync(expected.ID);

            // Then: The expected item is returned
            Assert.AreEqual(expected, model);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task DetailsNotFound()
        {
            // Given: A single item
            context.AddRange(Items.Take(1));

            // When: Attempting to get an item using an ID that is not in the db
            var maxid = context.Get<BudgetTx>().Max(x => x.ID);
            var badid = maxid + 1;
            var model = await repository.GetByIdAsync(badid);

            // Then: Exception is thrown
        }

        [TestMethod]
        public async Task DoesntExist()
        {
            // Given: A single item in the data set
            context.AddRange(Items.Take(1));

            // When: Testing whether an item exists using an ID that is not in the db
            var maxid = context.Get<BudgetTx>().Max(x => x.ID);
            var badid = maxid + 1;
            var exists = await repository.TestExistsByIdAsync(badid);

            // Then: The item is reported as not existing
            Assert.IsFalse(exists);
        }

        [TestMethod]
        public async Task Create()
        {
            // Given: A single item in the data set
            context.AddRange(Items.Take(1));

            // When: Adding a second item
            var expected = Items.Skip(1).First();
            await repository.AddAsync(expected);
            
            // Then: There are two items in the data set
            Assert.AreEqual(2, context.Get<BudgetTx>().Count());
        }

        [TestMethod]
        public async Task EditObjectValues()
        {
            // Given: One item in the dataset
            var items = Items.Skip(3).Take(1);
            context.AddRange(items);
            var id = items.First().ID;

            // When: Updating it with new values
            var updated = Items.Skip(1).First();
            updated.ID = id;
            await repository.UpdateAsync(updated);

            // Then: Still only one item in the dataset
            Assert.AreEqual(1, context.BudgetTxs.Count());

            // And: it's equal to our new one
            var actual = context.Get<BudgetTx>().Single();
            Assert.AreEqual(updated, actual);
            Assert.AreEqual(0, CompareKeys(updated, actual));
        }

        [TestMethod]
        public async Task DeleteConfirmed()
        {
            // Given: Five items in the data set
            context.AddRange(Items.Take(5));

            // When: Removing an item
            var expected = Items.Skip(3).First();
            await repository.RemoveAsync(expected);

            // Then: Only four items remain
            Assert.AreEqual(4, context.BudgetTxs.Count());

            // And: Removed item is not there
            Assert.IsFalse(context.Get<BudgetTx>().Any(x => CompareKeys(x,expected) == 0));
        }

        [TestMethod]
        public void Download()
        {
            // Given: Five items in the data set
            var expected = Items.Take(5).ToList();
            context.AddRange(expected);

            // When: Downloading the items as a spreadsheet
            using var stream = repository.AsSpreadsheet();

            // And: Deserializing items from the spreadsheet
            using var ssr = new SpreadsheetReader();
            ssr.Open(stream);
            var actual = ssr.Deserialize<BudgetTx>();

            // Then: The received items match the data set (sorted)
            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        private async Task<IEnumerable<BudgetTx>> WhenImportingItemsAsSpreadsheet(IEnumerable<BudgetTx> expected)
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
            var importer = new BudgetTxImporter(repository);
            importer.LoadFromXlsx(stream);
            return await importer.ProcessAsync();
        }

        [TestMethod]
        public async Task Upload()
        {
            // Given: A spreadsheet with five items
            var expected = Items.Take(5);

            // When: Importing it via an importer
            await WhenImportingItemsAsSpreadsheet(expected);

            // Then: The expected items are in the dataset
            Assert.IsTrue(context.Get<BudgetTx>().SequenceEqual(expected));
        }
        [TestMethod]
        public async Task UploadAddNewDuplicate()
        {
            // Given: Five items in the data set
            var expected = Items.Take(5).ToList();
            context.AddRange(expected);

            // When: Uploading three new items, one of which the same as an already existing item
            // NOTE: These items are not EXACTLY duplicates, just duplicate enough to trigger the
            // hashset equality constraint on input.
            var upload = Items.Skip(5).Take(2).Concat(await MakeDuplicateOf(expected.Take(1)));
            var actual = await WhenImportingItemsAsSpreadsheet(upload);

            // Then: Only two items were imported, because one exists
            Assert.AreEqual(2, actual.Count());

            // And: The data set now includes seven (not eight) items
            Assert.AreEqual(7, context.Get<BudgetTx>().Count());
        }
        [TestMethod]
        public async Task UploadMinmallyDuplicate()
        {
            // Given: Five items in the data set
            var expected = Items.Take(5).ToList();
            context.AddRange(expected);

            // When: Uploading three new items which are EXACTLY the same as existing items,
            // just that the amounts vary (which still counts them as duplicates here)
            var upload = await MakeDuplicateOf(expected.Take(3));
            foreach (var item in upload)
                item.Amount += 100m;

            var actual = await WhenImportingItemsAsSpreadsheet(upload);

            // Then: No items were uploaded
            Assert.AreEqual(0, actual.Count());

            // And: Data set still just has the five we originall uploaded
            Assert.IsTrue(context.Get<BudgetTx>().SequenceEqual(expected));
        }
        [TestMethod]
        public async Task Bug890()
        {
            // Bug 890: BudgetTxs upload fails to filter duplicates when source data has >2 digits
            // Hah, this is fixed by getting UploadMinmallyDuplicate() test to properly pass.

            // Given: Five items in the data set
            var expected = Items.Take(5).ToList();
            context.AddRange(expected);

            // When: Uploading one new items which is EXACTLY the same as existing items,
            // just that the amounts is off by $0.001
            var upload = await MakeDuplicateOf(expected.Take(1));
            upload.First().Amount += 0.001m;
            var actual = await WhenImportingItemsAsSpreadsheet(upload);

            // Then: No items were uploaded
            Assert.AreEqual(0, actual.Count());

            // And: Data set still just has the five we originally uploaded
            Assert.IsTrue(context.Get<BudgetTx>().SequenceEqual(expected));
        }

        /// <summary>
        /// Make an exact duplicate of these <paramref name="items"/>
        /// </summary>
        /// <param name="items">Items to copy</param>
        /// <returns>List of cloned items</returns>
        public async Task<List<BudgetTx>> MakeDuplicateOf(IEnumerable<BudgetTx> items)
        {
            var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream,items);
            stream.Seek(0, SeekOrigin.Begin);
            var result = await JsonSerializer.DeserializeAsync<List<BudgetTx>>(stream);
            return result;
        }
    }
}
