using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Importers;
using YoFi.Core.Repositories;
using YoFi.Tests.Helpers;
using YoFi.Tests.Common;

namespace YoFi.Tests.Core
{
    /// <summary>
    /// This class tests IRepository(<typeparamref name="T"/>)
    /// </summary>
    /// <remarks>
    /// Most repositories will have more functionality than only what is in the general
    /// iterface, so you'll want to cover that ground in the inherited class
    /// </remarks>
    /// <typeparam name="T">Model type contained in the repository under test</typeparam>
    [TestClass]
    public abstract class BaseRepositoryTest<T> where T: class, IModelItem, new()
    {
        protected IRepository<T> repository;
        protected MockDataContext context;

        /// <summary>
        /// Sample items to use in test
        /// </summary>
        /// <remarks>
        /// Generally, the first 5 are the most commonly used
        /// </remarks>
        protected abstract List<T> Items { get; }

        /// <summary>
        /// Comparison to sort keys
        /// </summary>
        /// <remarks>
        /// The idea here is that we order our sample data according to the method described
        /// here, so that if we sort it by this comparer, it equals the sort order given in
        /// IRepository(T).InDefaultOrder()
        /// </remarks>
        /// <param name="x">First item</param>
        /// <param name="y">Second item</param>
        /// <returns>-1 if <paramref name="x"/> comes before <paramref name="y"/></returns>
        protected abstract int CompareKeys(T x, T y);

        /// <summary>
        /// Create an importer suitable for importing these items
        /// </summary>
        /// <returns></returns>
        protected abstract BaseImporter<T> MakeImporter();

        protected async Task<IEnumerable<T>> WhenImportingItemsAsSpreadsheet(IEnumerable<T> expected)
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
            var importer = MakeImporter();
            importer.LoadFromXlsx(stream);
            return await importer.ProcessAsync();
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
            var maxid = context.Get<T>().Max(x => x.ID);
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
            Assert.AreEqual(2, context.Get<T>().Count());
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
            Assert.AreEqual(1, context.Get<T>().Count());

            // And: it's equal to our new one
            var actual = context.Get<T>().Single();
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
            Assert.AreEqual(4, context.Get<T>().Count());

            // And: Removed item is not there
            Assert.IsFalse(context.Get<T>().Any(x => CompareKeys(x, expected) == 0));
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
            var actual = ssr.Deserialize<T>();

            // Then: The received items match the data set (sorted)
            expected.Sort(CompareKeys);
            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public async Task Upload()
        {
            // Given: A spreadsheet with five items
            var expected = Items.Take(5);

            // When: Importing it via an importer
            await WhenImportingItemsAsSpreadsheet(expected);

            // Then: The expected items are in the dataset
            Assert.IsTrue(context.Get<T>().SequenceEqual(expected));
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
            var upload = Items.Skip(5).Take(2).Concat(await DeepCopy.MakeDuplicateOf(expected.Take(1)));
            var actual = await WhenImportingItemsAsSpreadsheet(upload);

            // Then: Only two items were imported, because one exists
            Assert.AreEqual(2, actual.Count());

            // And: The data set now includes seven (not eight) items
            Assert.AreEqual(7, context.Get<T>().Count());
        }
    }
}
