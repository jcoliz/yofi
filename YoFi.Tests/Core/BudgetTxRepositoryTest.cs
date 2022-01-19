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
using YoFi.Core.Repositories.Wire;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class BudgetTxRepositoryTest: BaseRepositoryTest<BudgetTx>
    {
        protected override List<BudgetTx> Items
        {
            get
            {
                return new List<BudgetTx>()
                {
                    new BudgetTx() { ID = 1, Timestamp = new System.DateTime(2020, 06, 01),  Category = "A", Amount = 200m },
                    new BudgetTx() { ID = 2, Timestamp = new System.DateTime(2020, 06, 01),  Category = "B", Amount = 300m },
                    new BudgetTx() { ID = 3, Timestamp = new System.DateTime(2020, 05, 01),  Category = "C", Amount = 700m },
                    new BudgetTx() { ID = 4, Timestamp = new System.DateTime(2020, 05, 01),  Category = "B", Amount = 600m },
                    new BudgetTx() { ID = 5, Timestamp = new System.DateTime(2020, 05, 01),  Category = "A", Amount = 500m },
                    new BudgetTx() { Timestamp = new System.DateTime(2020, 06, 01), Category = "C", Amount = 400m },
                    new BudgetTx() { Timestamp = new System.DateTime(2020, 07, 01), Category = "A", Amount = 100m },
                };
            }
        }

        BudgetTxRepository itemRepository => base.repository as BudgetTxRepository;

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
        protected override int CompareKeys(BudgetTx x, BudgetTx y) => x.Amount.CompareTo(y.Amount);

        [TestInitialize]
        public void SetUp()
        {
            context = new MockDataContext();
            repository = new BudgetTxRepository(context);
        }

        [TestMethod]
        public async Task IndexQ()
        {
            // Given: Five items in the data set
            var all = Items.Take(5);
            context.AddRange(all);

            // When: Querying items from the repository for a single category
            var lookup = all.ToLookup(x => x.Category, x=>x);
            var expected = lookup.First();
            var category = expected.Key;
            var qresult = await repository.GetByQueryAsync(new WireQueryParameters() { Query = category });

            // Then: Only the matching items are returned
            var items = expected.ToList();
            items.Sort(CompareKeys);
            Assert.IsTrue(qresult.Items.SequenceEqual(items));
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
            await itemRepository.BulkDeleteAsync();

            // Then: The number of items is total minus deleted
            Assert.AreEqual(numtotalitems - numdeleteditems, repository.All.Count());

            // And: All repository items are unselected
            Assert.IsTrue(repository.All.All(x => x.Selected != true));
        }


        [TestMethod]
        public async Task UploadMinmallyDuplicate()
        {
            // Given: Five items in the data set
            var expected = Items.Take(5).ToList();
            context.AddRange(expected);

            // When: Uploading three new items which are EXACTLY the same as existing items,
            // just that the amounts vary (which still counts them as duplicates here)
            var upload = await DeepCopy.MakeDuplicateOf(expected.Take(3));
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
            var upload = await DeepCopy.MakeDuplicateOf(expected.Take(1));
            upload.First().Amount += 0.001m;
            var actual = await WhenImportingItemsAsSpreadsheet(upload);

            // Then: No items were uploaded
            Assert.AreEqual(0, actual.Count());

            // And: Data set still just has the five we originally uploaded
            Assert.IsTrue(context.Get<BudgetTx>().SequenceEqual(expected));
        }

        [TestMethod]
        public virtual async Task Upload()
        {
            // Given: A spreadsheet with five items
            var expected = Items.Take(5);

            // When: Importing it via an importer
            await WhenImportingItemsAsSpreadsheet(expected);

            // Then: The expected items are in the dataset
            Assert.IsTrue(context.Get<BudgetTx>().SequenceEqual(expected));
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
            Assert.AreEqual(7, context.Get<BudgetTx>().Count());
        }

        protected async Task<IEnumerable<BudgetTx>> WhenImportingItemsAsSpreadsheet(IEnumerable<BudgetTx> expected)
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
            var importer = new BaseImporter<BudgetTx>(repository);
            importer.QueueImportFromXlsx(stream);
            return await importer.ProcessImportAsync();
        }

        #region Wire Interface

        //
        // This region contains tests which test the budget repository implementing the new Wire Repository
        // interface.
        //

        private async Task GivenItemsInRepository(int numitems)
        {
            var manyitems = Enumerable.Range(1, numitems).Select(x => new BudgetTx() { Amount = x * 100m, Category = x.ToString(), Timestamp = new DateTime(2020, 1, 1) + TimeSpan.FromDays(x) });
            await itemRepository.AddRangeAsync(manyitems);
        }

        [TestMethod]
        public async Task IndexPage1()
        {
            // Given: A very long set of items 
            var numitems = 100;
            await GivenItemsInRepository(numitems);

            // When: Calling Index page 1
            var result = await itemRepository.GetByQueryAsync(new WireQueryParameters());

            // And: Model has one page of items
            Assert.AreEqual(result.PageInfo.PageSize, result.Items.Count());
            Assert.AreEqual(result.PageInfo.NumItems, result.Items.Count());

            // And: Page Item values are as expected
            Assert.AreEqual(1, result.PageInfo.FirstItem);
            Assert.AreEqual(numitems, result.PageInfo.TotalItems);
        }

        [TestMethod]
        public async Task IndexPage2()
        {
            // Given: A long set of items, which is longer than one page, but not as long as two pages 
            var pagesize = await itemRepository.GetPageSizeAsync();
            var numitems = pagesize * 3 / 2;
            await GivenItemsInRepository(numitems);

            // When: Calling Index page 2
            var result = await itemRepository.GetByQueryAsync(new WireQueryParameters() { Page = 2 });

            // And: Only items after one page's worth of items are returned
            Assert.AreEqual(pagesize / 2, result.Items.Count());

            // And: Page Item values are as expected
            Assert.AreEqual(1 + pagesize, result.PageInfo.FirstItem);
            Assert.AreEqual(2, result.PageInfo.TotalPages);
            Assert.AreEqual(numitems, result.PageInfo.TotalItems);
        }

        [TestMethod]
        public async Task IndexQSubstring()
        {
            // Given: A mix of items, some with '{word}' in their category and some without
            await GivenItemsInRepository(11);

            // When: Calling index q={word}
            var word = "1";
            var result = await itemRepository.GetByQueryAsync(new WireQueryParameters() { Query = word });

            // Then: Only the expected items are returned
            var expected = repository.All.Where(x => x.Category.Contains(word));
            Assert.IsTrue(expected.SequenceEqual(result.Items));
        }

        #endregion
    }
}
