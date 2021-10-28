using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.Core.Repositories;
using YoFi.Core.Models;
using YoFi.Core.Importers;
using YoFi.Tests.Helpers;
using YoFi.Tests.Common;

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
        protected override int CompareKeys(BudgetTx x, BudgetTx y) => x.Amount.CompareTo(y.Amount);

        protected override BaseImporter<BudgetTx> MakeImporter() => new BaseImporter<BudgetTx>(repository);

        [TestInitialize]
        public void SetUp()
        {
            context = new MockDataContext();
            repository = new BudgetTxRepository(context);
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
    }
}
