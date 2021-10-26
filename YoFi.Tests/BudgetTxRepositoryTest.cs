using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.AspNet.Core.Repositories;
using YoFi.AspNet.Models;
using YoFi.Core;

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
            Assert.IsNotNull(repository);
        }

        [TestMethod]
        public void IndexEmpty()
        {
            var model = repository.ForQuery(null);
            Assert.AreEqual(0, model.Count());
        }

        [TestMethod]

        public void IndexSingle()
        {
            var expected = Items.Take(1);
            context.AddRange(expected);

            var model = repository.ForQuery(null);

            Assert.IsTrue(model.SequenceEqual(expected));
        }

        [TestMethod]
        public void IndexMany()
        {
            var expected = Items.Take(5).ToList();
            context.AddRange(expected);

            var model = repository.ForQuery(null);

            // Sort the original items by Key
            expected.Sort((x, y) => x.Amount.CompareTo(y.Amount));

            // Test that the resulting items are in the same order
            Assert.IsTrue(model.SequenceEqual(expected));
        }

        [TestMethod]
        public async Task DetailsFound()
        {
            context.AddRange(Items.Take(5));

            var expected = Items.Skip(3).First();
            var model = await repository.GetByIdAsync(expected.ID);

            Assert.AreEqual(expected, model);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task DetailsNotFound()
        {
            context.AddRange(Items.Take(1));
            var maxid = context.BudgetTxs.Max(x => x.ID);
            var badid = maxid + 1;

            var model = await repository.GetByIdAsync(badid);
        }

        [TestMethod]
        public async Task Create()
        {
            context.AddRange(Items.Take(1));

            var expected = Items.Skip(1).First();
            await repository.AddAsync(expected);
            
            Assert.AreEqual(2, context.BudgetTxs.Count());
        }

        [TestMethod]
        public async Task EditObjectValues()
        {
            var items = Items.Skip(3).Take(1);
            context.AddRange(items);
            var id = items.First().ID;

            var updated = Items.Skip(1).First();
            updated.ID = id;

            await repository.UpdateAsync(updated);

            // Still only one item in the db
            Assert.AreEqual(1, context.BudgetTxs.Count());

            // And it's equal to our new one
            Assert.AreEqual(updated.Amount, context.BudgetTxs.Single().Amount);
        }

        [TestMethod]
        public async Task DeleteConfirmed()
        {
            context.AddRange(Items.Take(5));

            var expected = Items.Skip(3).First();
            await repository.RemoveAsync(expected);

            Assert.AreEqual(4, context.BudgetTxs.Count());
        }
    }

    public class MockDataContext : IDataContext
    {
        public List<BudgetTx> BudgetTxData { get; } = new List<BudgetTx>();

        public IQueryable<Transaction> Transactions => throw new NotImplementedException();

        public IQueryable<Split> Splits => throw new NotImplementedException();

        public IQueryable<Payee> Payees => throw new NotImplementedException();

        public IQueryable<BudgetTx> BudgetTxs => BudgetTxData.AsQueryable();

        public Task AddAsync(object item)
        {
            if (item == null)
                throw new ArgumentException("Expected non-null item");

            if (item.GetType() == typeof(BudgetTx))
            {
                BudgetTxData.Add(item as BudgetTx);
            }
            else
                throw new NotImplementedException();

            return Task.CompletedTask;
        }

        public void AddRange(IEnumerable<object> items)
        {
            if (items == null)
                throw new ArgumentException("Expected non-null items");

            if (!items.Any())
                return;

            if (items.First().GetType() == typeof(BudgetTx))
            {
                BudgetTxData.AddRange(items as IEnumerable<BudgetTx>);
            }
            else
                throw new NotImplementedException();
        }

        public Task AddRangeAsync(IEnumerable<object> items)
        {
            throw new NotImplementedException();
        }

        public void Remove(object item)
        {
            if (item == null)
                throw new ArgumentException("Expected non-null item");

            if (item.GetType() == typeof(BudgetTx))
            {
                var btx = item as BudgetTx;

                var index = BudgetTxData.FindIndex(x => x.ID == btx.ID);
                BudgetTxData.RemoveAt(index);
            }
            else
                throw new NotImplementedException();
        }

        public Task SaveChangesAsync()
        {
            return Task.CompletedTask;
        }

        public void Update(object item)
        {
            if (item == null)
                throw new ArgumentException("Expected non-null item");

            if (item.GetType() == typeof(BudgetTx))
            {
                var btx = item as BudgetTx;

                var index = BudgetTxData.FindIndex(x => x.ID == btx.ID);
                BudgetTxData[index] = btx;
            }
            else
                throw new NotImplementedException();
        }
    }
}
