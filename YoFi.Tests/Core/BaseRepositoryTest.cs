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
    /// <summary>
    /// This class tests IRepository(<typeparamref name="T"/>)
    /// </summary>
    /// <remarks>
    /// Most repositories will have more functionality than only what is in the general
    /// iterface, so you'll want to cover that ground in the inherited class
    /// </remarks>
    /// <typeparam name="T">Model type contained in the repository under test</typeparam>
    [TestClass]
    public abstract class BaseRepositoryTest<T> where T: class, IModelItem<T>, new()
    {
        #region Fields

        protected IRepository<T> repository;
        protected MockDataContext context;

        #endregion

        #region Properties

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

        #endregion

        #region Helpers


        protected virtual IEnumerable<TItem> GivenFakeItems<TItem>(int num, Func<TItem, TItem> func = null, int from = 1) where TItem : class, new()
        {
            return Enumerable
                .Range(from, num)
                .Select(x => GivenFakeItem<TItem>(x))
                .Select(func ?? (x => x));
        }

        protected virtual TItem GivenFakeItem<TItem>(int index) where TItem : class, new()
        {
            var result = new TItem();
            var properties = typeof(TItem).GetProperties();
            var chosen = properties.Where(x => x.CustomAttributes.Any(y => y.AttributeType == typeof(YoFi.Core.Models.Attributes.EditableAttribute)));

            foreach (var property in chosen)
            {
                var t = property.PropertyType;
                object o = default;

                if (t == typeof(string))
                    o = $"{property.Name} {index:D5}";
                else if (t == typeof(decimal))
                    o = index * 100m;
                else if (t == typeof(DateTime))
                    // Note that datetimes should descend, because anything which sorts by a datetime
                    // will typically sort descending
                    o = new DateTime(2001, 12, 31) - TimeSpan.FromDays(index);
                else
                    throw new NotImplementedException();

                property.SetValue(result, o);
            }

            // Not sure of a more generic way to handle this
            if (result is Transaction tx)
                tx.Splits = new List<Split>();

            return result;
        }

        protected async Task<(IEnumerable<T>, IEnumerable<T>)> GivenFakeDataInDatabase<TIgnored>(int total, int selected, Func<T, T> func = null)
        {
            var firstitem = repository.All.Count() + 1;
            var numunchanged = total - selected;
            var items = GivenFakeItems<T>(numunchanged,null,from:firstitem).Concat(GivenFakeItems<T>(selected,func,from:numunchanged + firstitem)).ToList();
            var wasneeded = items.Skip(numunchanged).Take(selected);

            await repository.AddRangeAsync(items);

            return (items, wasneeded);
        }

        protected async Task<IEnumerable<T>> GivenFakeDataInDatabase<TIgnored>(int total)
        {
            (var result, _) = await GivenFakeDataInDatabase<TIgnored>(total, 0);
            return result;
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
            var expected = Items.Take(1);
            context.AddRange(expected);

            // When: Querying items from the repository
            var qresult = await repository.GetByQueryAsync(new WireQueryParameters());

            // Then: The results match what we added to the data set
            Assert.IsTrue(qresult.Items.SequenceEqual(expected));
        }

        [TestMethod]
        public async Task IndexMany()
        {
            var expected = Items.Take(5).ToList();
            context.AddRange(expected);

            var qresult = await repository.GetByQueryAsync(new WireQueryParameters());

            // Test that the resulting items are the same as expected items ordered correctly
            expected.Sort(CompareKeys);
            Assert.IsTrue(qresult.Items.SequenceEqual(expected));
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
        public async Task RemoveRange()
        {
            // Given: Five items in the data set
            var items = Items.Take(5);
            context.AddRange(items);

            // When: Removing multiple items
            var expected = items.Skip(3);
            await repository.RemoveRangeAsync(expected);

            // Then: Only three items remain
            Assert.AreEqual(3, context.Get<T>().Count());

            // And: Removed items are not there
            Assert.IsFalse(context.Get<T>().Any(x => expected.Any(y=> CompareKeys(x, y) == 0)));
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
