using Common.DotNet.Test;
using jcoliz.FakeObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Core.Repositories.Wire;
using YoFi.Tests.Helpers;

namespace YoFi.Core.Tests.Unit
{
    [TestClass]
    public class BudgetTxRepositoryTest: BaseRepositoryTest<BudgetTx>
    {
        BudgetTxRepository itemRepository => base.repository as BudgetTxRepository;

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
            var items = FakeObjects<BudgetTx>.Make(5).SaveTo(this);
            var expected = items.Last();

            // When: Querying items from the repository for a single category
            var category = expected.Category;
            var qresult = await repository.GetByQueryAsync(new WireQueryParameters() { Query = category });

            // Then: Only the matching item is returned
            Assert.AreEqual(expected,qresult.Items.Single());
        }

        [TestMethod]
        public async Task BulkDelete()
        {
            // Given: Many items in the data set, some of which are selected
            var data = FakeObjects<BudgetTx>.Make(5).Add(4, x => x.Selected = true).SaveTo(this);
            var expected = data.Group(0);

            // When: Bulk deleting the selected items
            await itemRepository.BulkDeleteAsync();

            // Then: Only the expected items remain
            Assert.IsTrue(repository.All.SequenceEqual(expected));

            // And: All repository items are unselected
            Assert.IsTrue(repository.All.All(x => x.Selected != true));
        }

        [TestMethod]
        public async Task UploadMinmallyDuplicate()
        {
            // Given: Many items in the data set, some of which we care about
            var data = FakeObjects<BudgetTx>.Make(5).Add(4).SaveTo(this);

            // When: Uploading three new items which are EXACTLY the same as existing items,
            // just that the amounts vary (which still counts them as duplicates here)
            var chosen = data.Group(1);
            var upload = await DeepCopy.MakeDuplicateOf(chosen);
            foreach (var item in upload)
                item.Amount += 100m;

            var actual = await WhenImportingItemsAsSpreadsheet(upload);

            // Then: No items were uploaded
            Assert.AreEqual(0, actual.Count());

            // And: Data set still all the original items
            Assert.IsTrue(context.Get<BudgetTx>().SequenceEqual(data));
        }
        [TestMethod]
        public async Task Bug890()
        {
            // Bug 890: BudgetTxs upload fails to filter duplicates when source data has >2 digits
            // Hah, this is fixed by getting UploadMinmallyDuplicate() test to properly pass.

            // Given: Many items in the data set, some of which we care about
            var data = FakeObjects<BudgetTx>.Make(5).Add(4).SaveTo(this);

            // When: Uploading one new items which is EXACTLY the same as existing items,
            // just that the amounts is off by $0.001
            var chosen = data.Group(1);
            var upload = await DeepCopy.MakeDuplicateOf(chosen);
            foreach (var item in upload)
                item.Amount += 0.001m;

            var actual = await WhenImportingItemsAsSpreadsheet(upload);

            // Then: No items were uploaded
            Assert.AreEqual(0, actual.Count());

            // And: Data set still all the original items
            Assert.IsTrue(context.Get<BudgetTx>().SequenceEqual(data));
        }

        [TestMethod]
        public virtual async Task Upload()
        {
            // Given: A spreadsheet with five items
            var expected = FakeObjects<BudgetTx>.Make(5);

            // When: Importing it via an importer
            await WhenImportingItemsAsSpreadsheet(expected);

            // Then: The expected items are in the dataset
            Assert.IsTrue(context.Get<BudgetTx>().SequenceEqual(expected));
        }

        [TestMethod]
        public virtual async Task UploadAddNewDuplicate()
        {
            // Given: Five items in the data set, some of which we care about, and two more extra items
            var data = FakeObjects<BudgetTx>.Make(4).Add(1).SaveTo(this).Add(2);

            // When: Uploading three new items, one of which the same as an already existing item
            // NOTE: These items are not EXACTLY duplicates, just duplicate enough to trigger the
            // hashset equality constraint on input.
            var duplicated = data.Group(1);
            var added = data.Group(2);
            var upload = added.Concat(await DeepCopy.MakeDuplicateOf(duplicated));
            var actual = await WhenImportingItemsAsSpreadsheet(upload);

            // Then: Only two items were imported, because one exists
            Assert.IsTrue(actual.SequenceEqual(added));

            // And: The data set the entire expected data set, which does not include the duplicated item
            Assert.IsTrue(context.Get<BudgetTx>().SequenceEqual(data));
        }

        #region Wire Interface

        [TestMethod]
        public async Task IndexQSubstring()
        {
            // Given: A mix of items, some with '{word}' in their category and some without
            var word = "IndexQSubstring";
            var expected = FakeObjects<BudgetTx>.Make(5).Add(6,x=>x.Category += word).SaveTo(this).Group(1);

            // When: Calling index q={word}
            var result = await itemRepository.GetByQueryAsync(new WireQueryParameters() { Query = word });

            // Then: Only the expected items are returned
            Assert.IsTrue(expected.SequenceEqual(result.Items));
        }

        #endregion
    }
}
