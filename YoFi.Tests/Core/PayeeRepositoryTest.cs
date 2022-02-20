using Common.DotNet.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class PayeeRepositoryTest : BaseRepositoryTest<Payee>
    {
        #region Fields

        PayeeRepository itemRepository => base.repository as PayeeRepository;

        #endregion

        #region Init/Cleanup

        [TestInitialize]
        public void SetUp()
        {
            context = new MockDataContext();
            repository = new PayeeRepository(context);
        }

        #endregion

        #region Tests

        [TestMethod]
        public async Task BulkEdit()
        {
            // Given: Many items in the data set, some of which are selected
            var data = FakeObjects<Payee>.Make(5).Add(4, x => x.Selected = true).SaveTo(this);
            var untouched = data.Group(0);
            var selected = data.Group(1);

            // When: Applying bulk edit to the repository using a new category
            var newcategory = "New Category";
            await itemRepository.BulkEditAsync(newcategory);

            // Then: The selected items have the new category
            Assert.IsTrue(selected.All(x => x.Category == newcategory));

            // And: The other items are unchanged
            Assert.IsTrue(untouched.All(x => x.Category != newcategory));

            // And: All items are unselected
            Assert.IsTrue(data.All(x => x.Selected != true));
        }

        [TestMethod]
        public async Task BulkEditNoChange()
        {
            // Given: Many items in the data set, some of which are selected
            var data = FakeObjects<Payee>.Make(5).Add(4, x => x.Selected = true).SaveTo(this);

            // And: Taking a snapshot of that data for later comparison
            var expected = await DeepCopy.MakeDuplicateOf(data);

            // When: Applying bulk edit to the repository using null
            await itemRepository.BulkEditAsync(null);

            // And: All items are unchanged
            Assert.IsTrue(expected.SequenceEqual(repository.All));

            // And: All items are unselected
            Assert.IsTrue(data.All(x => x.Selected != true));
        }

        [TestMethod]
        public async Task BulkDelete()
        {
            // Given: Many items in the data set, some of which are selected
            var data = FakeObjects<Payee>.Make(5).Add(4, x => x.Selected = true).SaveTo(this);
            var expected = data.Group(0);

            // When: Bulk deleting the selected items
            await itemRepository.BulkDeleteAsync();

            // Then: Only the expected items remain
            Assert.IsTrue(repository.All.SequenceEqual(expected));

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
            var actual = await itemRepository.NewFromTransactionAsync(transaction.ID);

            // Then: The resulting payee matches category & payee name
            Assert.AreEqual(payee, actual.Name);
            Assert.AreEqual(category, actual.Category);
        }

        [TestMethod]
        public virtual async Task Upload()
        {
            // Given: A spreadsheet with five items
            var expected = FakeObjects<Payee>.Make(5);

            // When: Importing it via an importer
            await WhenImportingItemsAsSpreadsheet(expected);

            // Then: The expected items are in the dataset
            Assert.IsTrue(context.Get<Payee>().SequenceEqual(expected));
        }

        [TestMethod]
        public virtual async Task UploadAddNewDuplicate()
        {
            // Given: Five items in the data set, some of which we care about, and two more extra items
            var data = FakeObjects<Payee>.Make(4).Add(1).SaveTo(this).Add(2);

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
            Assert.IsTrue(context.Get<Payee>().SequenceEqual(data));
        }

        [TestMethod]
        public async Task BulkEditParts()
        {
            // Given: A list of items with varying categories, some of which match the pattern *:B:*

            var categories = new string[] { "AB:Second:E", "AB:Second:E:F", "AB:Second:A:B:C", "G H:Second:KLM NOP" };
            await repository.AddRangeAsync(categories.Select(x => new Payee() { Category = x, Name = x, Selected = true }));

            // When: Calling Bulk Edit with a new category which includes positional wildcards
            var newcategory = "(1):New Category:(3+)";
            await itemRepository.BulkEditAsync(newcategory);

            // Then: All previously-selected items are now correctly matching the expected category
            Assert.IsTrue(categories.Select(x => x.Replace("Second", "New Category")).SequenceEqual(repository.All.Select(x => x.Category)));
        }

        [DataRow("", null)]
        [DataRow("Bogus", null)]
        [DataRow("NameXX","CategoryXX")]
        [DataRow("Regex11", "CategoryRegex")]
        [DataRow("Regex1A", null)]
        [DataTestMethod]
        public async Task GetCategoryMatchingPayee(string name, string expected)
        {
            // Given: A payee which maps a known name to a known payee
            FakeObjects<Payee>
                .Make(5)
                .Add(1, x => { x.Name = "NameXX"; x.Category = "CategoryXX"; } )
                .Add(1, x => { x.Name = "/Regex[0-9][0-9]/"; x.Category = "CategoryRegex"; })
                .SaveTo(this);

            // When: Looking for a match
            var actual = await itemRepository.GetCategoryMatchingPayeeAsync(name);

            // Then: Expected result found
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task PayeeCache()
        {
            // Given: A payee which maps a known name to a known payee
            FakeObjects<Payee>
                .Make(5)
                .Add(1, x => { x.Name = "NameXX"; x.Category = "CategoryXX"; })
                .Add(1, x => { x.Name = "/Regex[0-9][0-9]/"; x.Category = "CategoryRegex"; })
                .SaveTo(this);

            // When: Caching the payees
            await itemRepository.LoadCacheAsync();

            // And: Looking for a match
            await GetCategoryMatchingPayee("Regex11", "CategoryRegex");

            // Then: No problems
        }

        #endregion

        #region Payee Object Test(s)

        [TestMethod]
        public void PayeeHashEquals()
        {
            // Given: Two memberwise identical payees
            var one = FakeObjects<Payee>.Make(1).Single();
            var two = FakeObjects<Payee>.Make(1).Single();

            // When: Adding both to a hashset of payees
            var hashset = new HashSet<Payee>() { one, two };

            // Then: Only one added, because they're functionally duplicates
            Assert.AreEqual(1, hashset.Count);
        }

        #endregion
    }
}
