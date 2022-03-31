using Common.DotNet.Test;
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
using YoFi.Core.Repositories.Wire;
using YoFi.Tests.Helpers;

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
    public abstract class BaseRepositoryTest<T>: IFakeObjectsSaveTarget where T: class, IModelItem<T>, new()
    {
        #region Fields

        protected IRepository<T> repository;
        protected MockDataContext context;

        #endregion

        #region Helpers

        public void AddRange(System.Collections.IEnumerable objects)
        {
            if (objects is IEnumerable<T> items)
            {
                repository.AddRangeAsync(items).Wait();
            }
        }

        /// <summary>
        /// Alias for GetByQueryAsync
        /// </summary>
        /// <remarks>
        /// This is to enable copy/paste integration tests
        /// </remarks>
        /// <param name="parms"></param>
        /// <returns></returns>
        protected Task<IWireQueryResult<T>> WhenGettingIndex(IWireQueryParameters parms)
            => repository.GetByQueryAsync(parms);

        protected async Task<IEnumerable<TItem>> WhenImportingItemsAsSpreadsheet<TItem>(IEnumerable<TItem> expected) where TItem: class, IImportDuplicateComparable, IModelItem<TItem>, new()
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
            if (repository is IRepository<TItem> tr)
            {
                var importer = new BaseImporter<TItem>(tr);
                importer.QueueImportFromXlsx(stream);
                return await importer.ProcessImportAsync();
            }
            else
                throw new NotImplementedException();
        }

        protected void ThenResultsAreEqualByTestKey(IWireQueryResult<T> result, IEnumerable<T> chosen)
        {
            Assert.IsTrue(result.Items.SequenceEqual(chosen));
        }

        #endregion

        #region Tests

        [TestMethod]
        public void Empty()
        {
            // When: Creating a new repository (Done in setup)

            // Then: Repository created successfully
            Assert.IsNotNull(repository);
        }

        [TestMethod]
        public async Task IndexEmpty()
        {
            // Given: No items in the repository

            // When: Querying items from the repository
            var qresult = await repository.GetByQueryAsync(new WireQueryParameters());

            // Then: No items returned
            Assert.AreEqual(0, qresult.Items.Count());
        }

        [TestMethod]

        public async Task IndexSingle()
        {
            // Given: A single item in the data set
            var expected = FakeObjects<T>.Make(1).SaveTo(this);

            // When: Querying items from the repository
            var qresult = await repository.GetByQueryAsync(new WireQueryParameters());

            // Then: The results match what we added to the data set
            Assert.IsTrue(qresult.Items.SequenceEqual(expected));
        }

        [TestMethod]
        public async Task IndexMany()
        {
            // Given: Many items in the data set
            var expected = FakeObjects<T>.Make(5).SaveTo(this);

            // When: Querying items from the repository
            var qresult = await repository.GetByQueryAsync(new WireQueryParameters());

            // Test that the resulting items are the same as expected items
            // (Note that FakeObjects makes items in the correct order. If that wasn't the case, we'd need to order before comparing)
            Assert.IsTrue(qresult.Items.SequenceEqual(expected));
        }

        [TestMethod]
        public async Task IndexPage1()
        {
            // Given: A long set of items, which is longer than one page, but not as long as two pages 
            var pagesize = 25; // BaseRepository<T>.DefaultPageSize;
            var data = FakeObjects<T>.Make(pagesize).Add(pagesize/2).SaveTo(this);

            // When: Getting the Index
            var document = await WhenGettingIndex(new WireQueryParameters());

            // Then: Only one page of items returned, which are the LAST group, cuz it's sorted by time
            ThenResultsAreEqualByTestKey(document, data.Group(0));
        }

        [TestMethod]
        public async Task IndexPageTooMany()
        {
            // Given: A long set of items, which is longer than one page, but not as long as two pages 
            var pagesize = 25; // BaseRepository<T>.DefaultPageSize;
            var data = FakeObjects<T>.Make(pagesize).Add(pagesize / 2).SaveTo(this);

            // When: Getting the Index for page 3
            var document = await WhenGettingIndex(new WireQueryParameters() { Page = 3 });

            // Then: No items returned
            ThenResultsAreEqualByTestKey(document, Enumerable.Empty<T>());

            // And: Page info also says no items
            Assert.AreEqual(0, document.PageInfo.NumItems);
            Assert.AreEqual(0, document.PageInfo.FirstItem);

            // And: Acknowledge that there really should only be two pages
            Assert.AreEqual(2, document.PageInfo.TotalPages);
        }

        [TestMethod]
        public async Task DetailsFound()
        {
            // Given: Five items in the data set
            var items = FakeObjects<T>.Make(5).SaveTo(this);
            var expected = items.Last();
            var id = expected.ID;

            // When: Getting a single item by its ID
            var model = await repository.GetByIdAsync(id);

            // Then: The expected item is returned
            Assert.AreEqual(expected, model);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task DetailsNotFound()
        {
            // Given: A single item
            _ = FakeObjects<T>.Make(1).SaveTo(this);

            // When: Attempting to get an item using an ID that is not in the db
            var maxid = context.Get<T>().Max(x => x.ID);
            var badid = maxid + 1;
            var model = await repository.GetByIdAsync(badid);

            // Then: Exception is thrown
        }

        [TestMethod]
        public async Task DoesntExist()
        {
            // Given: A single item in the data set
            _ = FakeObjects<T>.Make(1).SaveTo(this);

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
            var data = FakeObjects<T>.Make(1).SaveTo(this);

            // When: Adding a second item
            var expected = data.Add(1).Last();
            await repository.AddAsync(expected);

            // Then: There are two items in the data set
            Assert.AreEqual(2, context.Get<T>().Count());
        }

        [TestMethod]
        public async Task EditObjectValues()
        {
            // Given: One item in the dataset
            var data = FakeObjects<T>.Make(1).SaveTo(this);
            var id = data.First().ID;

            // When: Updating it with new values
            var updated = data.Add(1).Last();
            updated.ID = id;
            await repository.UpdateAsync(updated);

            // Then: Still only one item in the dataset
            Assert.AreEqual(1, context.Get<T>().Count());

            // And: it's equal to our new one
            var actual = context.Get<T>().Single();
            Assert.AreEqual(updated, actual);
        }

        [TestMethod]
        public async Task DeleteConfirmed()
        {
            // Given: Many items in the data set
            var data = FakeObjects<T>.Make(4).Add(1).SaveTo(this);

            // When: Removing an item
            var removed = data.Group(1).Single();
            await repository.RemoveAsync(removed);

            // Then: Only first group of data remains
            Assert.IsTrue(context.Get<T>().SequenceEqual(data.Group(0)));
        }

        [TestMethod]
        public async Task RemoveRange()
        {
            // Given: Many items in the data set
            var data = FakeObjects<T>.Make(5).Add(10).SaveTo(this);

            // When: Removing multiple items
            var removed = data.Group(1);
            await repository.RemoveRangeAsync(removed);

            // Then: Only first group of data remains
            Assert.IsTrue(context.Get<T>().SequenceEqual(data.Group(0)));
        }

        [TestMethod]
        public void Download()
        {
            // Given: Many items in the data set
            var expected = FakeObjects<T>.Make(5).SaveTo(this).ToList();

            // When: Downloading the items as a spreadsheet
            using var stream = repository.AsSpreadsheet();

            // And: Deserializing items from the spreadsheet
            using var ssr = new SpreadsheetReader();
            ssr.Open(stream);
            var actual = ssr.Deserialize<T>();

            // Then: The received items match the data set
            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public async Task SetpageSize()
        {
            // When: Setting the pagesize
            var currentsize = await repository.GetPageSizeAsync();
            var newsize = currentsize * 10;
            await repository.SetPageSizeAsync(newsize);
            
            // Then: The page size was set properly
            var actual = await repository.GetPageSizeAsync();
            Assert.AreEqual(newsize, actual);
        }

        #endregion

    }
}
