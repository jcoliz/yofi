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
    public abstract class BaseRepositoryTest<T>: IFakeObjectsSaveTarget where T: class, IModelItem<T>, new()
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

        public void AddRange(System.Collections.IEnumerable objects)
        {
            if (objects is IEnumerable<T> items)
            {
                repository.AddRangeAsync(items).Wait();
            }
        }

        protected static IEnumerable<TItem>[] GivenFakeItemGroups<TItem>(IEnumerable<(int num, Func<TItem, TItem> func)> groups, int from = 1) where TItem : class, new()
        {
            var result = new List<IEnumerable<TItem>>();
            int starting = from;
            foreach(var group in groups)
            {
                result.Add(GivenFakeItems(group.num, group.func, starting).ToList());
                starting += group.num;
            }
            return result.ToArray();
        }

        protected static IEnumerable<TItem> GivenFakeItems<TItem>(int num, Func<TItem, TItem> func = null, int from = 1) where TItem : class, new()
        {
            return Enumerable
                .Range(from, num)
                .Select(x => GivenFakeItem<TItem>(x))
                .Select(func ?? (x => x));
        }

        protected static TItem GivenFakeItem<TItem>(int index) where TItem : class, new()
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
            var groups = GivenFakeItemGroups(new (int,Func<T,T>)[] { (numunchanged,null), (selected,func) },firstitem);
            var items = groups.SelectMany(x => x);
            var wasneeded = groups[1];

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
            var expected = FakeObjects<T>.Make(5).SaveTo(this).ToList();

            // When: Querying items from the repository
            var qresult = await repository.GetByQueryAsync(new WireQueryParameters());

            // Test that the resulting items are the same as expected items ordered correctly
            expected.Sort(CompareKeys);
            Assert.IsTrue(qresult.Items.SequenceEqual(expected));
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
            var maxid = context.Get<BudgetTx>().Max(x => x.ID);
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
            Assert.AreEqual(0, CompareKeys(updated, actual));
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
