using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private BudgetTxRepository repository;
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
                };
            }
        }

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
            Assert.IsTrue(expected.OrderBy(x=>x.Amount).SequenceEqual(model));
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
            Assert.AreEqual(updated.Amount, context.Get<BudgetTx>().Single().Amount);
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
            Assert.IsFalse(context.Get<BudgetTx>().Any(x => x.Amount == expected.Amount));
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

        [TestMethod]
        public async Task Upload()
        {
            // Given: A spreadsheet with five items
            var expected = Items.Take(5);
            using var stream = new MemoryStream();
            {
                using var ssw = new SpreadsheetWriter();
                ssw.Open(stream);
                ssw.Serialize(expected);
            }
            stream.Seek(9, SeekOrigin.Begin);

            // When: Importing it via an importer
            var importer = new BudgetTxImporter(repository);
            importer.LoadFromXlsx(stream);
            await importer.ProcessAsync();

            // Then: The expected items are in the dataset
            Assert.IsTrue(context.Get<BudgetTx>().SequenceEqual(expected));
        }
    }
}
