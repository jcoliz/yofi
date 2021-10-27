using Common.AspNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Controllers;
using YoFi.AspNet.Core.Repositories;
using YoFi.AspNet.Models;

namespace YoFi.Tests.Controllers.Slim
{
    /// <summary>
    /// Test the budget tx controller in the slimmest way possible
    /// </summary>
    /// <remarks>
    /// This variation of controller test mocks out the underlying repository
    /// </remarks>
    [TestClass]
    public class BudgetTxControllerSlimTest
    {
        BudgetTxsController controller;
        MockBudgetTxRepository repository;

        [TestInitialize]
        public void SetUp()
        {
            repository = new MockBudgetTxRepository();
            controller = new BudgetTxsController(repository);
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public async Task IndexEmpty()
        {
            // Given: Empty repository

            // When: Fetching the index
            var actionresult = await controller.Index();

            // Then: View is returned
            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);

            // And: Correct kind of model is returned 
            var model = Assert.That.IsOfType<IEnumerable<BudgetTx>>(viewresult.Model);

            // And: Model is empty
            Assert.AreEqual(0, model.Count());
        }

        [TestMethod]
        public async Task IndexPage1()
        {
            // Given: A very long set of items 
            var numitems = 100;
            repository.AddItems(numitems);

            // When: Calling Index page 1
            var actionresult = await controller.Index(p: 1);

            // Then: View is returned
            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);

            // And: Correct kind of model is returned 
            var model = Assert.That.IsOfType<IEnumerable<BudgetTx>>(viewresult.Model);

            // And: Model has one page of items
            Assert.AreEqual(BudgetTxsController.PageSize, model.Count());

            // And: Page Item values are as expected
            var pages = viewresult.ViewData[nameof(PageDivider)] as PageDivider;
            Assert.AreEqual(1, pages.PageFirstItem);
            Assert.AreEqual(BudgetTxsController.PageSize, pages.PageLastItem);
            Assert.AreEqual(numitems, pages.PageTotalItems);
        }

        [TestMethod]
        public async Task IndexPage2()
        {
            // Given: A long set of items, which is longer than one page, but not as long as two pages 
            var itemcount = BudgetTxsController.PageSize + PayeesController.PageSize / 2;
            repository.AddItems(itemcount);

            // When: Calling Index page 2
            var actionresult = await controller.Index(p: 2);

            // Then: View is returned
            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);

            // And: Correct kind of model is returned 
            var model = Assert.That.IsOfType<List<BudgetTx>>(viewresult.Model);

            // And: Only items after one page's worth of items are returned
            Assert.AreEqual(BudgetTxsController.PageSize / 2, model.Count);

            // And: Page Item values are as expected
            var pages = viewresult.ViewData[nameof(PageDivider)] as PageDivider;
            Assert.AreEqual(1 + BudgetTxsController.PageSize, pages.PageFirstItem);
            Assert.AreEqual(itemcount, pages.PageLastItem);
            Assert.AreEqual(itemcount, pages.PageTotalItems);
        }

        [TestMethod]
        public async Task IndexQSubstring()
        {
            // Given: A mix of items, some with '{word}' in their category and some without
            repository.AddItems(11);

            // When: Calling index q={word}
            var word = "1";
            var actionresult = await controller.Index(q: word);
            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);
            var model = Assert.That.IsOfType<List<BudgetTx>>(viewresult.Model);

            // Then: Only the exoected items are returned
            var expected = repository.All.Where(x => x.Category.Contains(word));
            Assert.IsTrue(expected.SequenceEqual(model));
        }

        [TestMethod]
        public async Task DetailsFound()
        {
            // Given: Five items in the respository
            repository.AddItems(5);

            // When: Retrieving details for a selected item
            var selected = repository.All.Skip(1).First();
            var actionresult = await controller.Details(selected.ID);
            Assert.That.ActionResultOk(actionresult);
            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);
            var model = Assert.That.IsOfType<BudgetTx>(viewresult.Model);

            // Then: The selected item is the one returned
            Assert.AreEqual(selected, model);
        }

        [TestMethod]
        public async Task DetailsNotFound()
        {
            // Given: Five items in the respository
            int numitems = 5;
            repository.AddItems(numitems);

            // When: Retrieving details for an ID which does not exist
            var actionresult = await controller.Details(numitems + 1);
            Assert.That.ActionResultOk(actionresult);

            // Then: Returns not found result
            var nfresult = Assert.That.IsOfType<NotFoundResult>(actionresult);
            Assert.AreEqual(404, nfresult.StatusCode);
        }

        [TestMethod]
        public async Task DetailsFailed()
        {
            // Given: Respository in failure state
            repository.Ok = false;

            // And: Five items in the respository
            repository.AddItems(5);

            // When: Retrieving details for a selected item
            var actionresult = await controller.Details(1);

            // Then: Returns status code 500 object result
            var nfresult = Assert.That.IsOfType<ObjectResult>(actionresult);
            Assert.AreEqual(500, nfresult.StatusCode);
        }

        [TestMethod]
        public void CreateInitial()
        {
            // When: Calling Create
            var actionresult = controller.Create();
            Assert.That.ActionResultOk(actionresult);
            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);

            // Then: It returns an empty model
            Assert.IsNull(viewresult.Model);
        }

        [TestMethod]
        public async Task Create()
        {
            // Given: Five items in the respository
            repository.AddItems(5);

            // When: Adding a new item
            var expected = MockBudgetTxRepository.MakeItem(6);
            var actionresult = await controller.Create(expected);
            Assert.That.ActionResultOk(actionresult);

            // Then: Returns a redirection to Index
            var redirresult = Assert.That.IsOfType<RedirectToActionResult>(actionresult);
            Assert.AreEqual("Index", redirresult.ActionName);

            // And: There are now 6 items in the repository
            Assert.AreEqual(6, repository.All.Count());

            // And: The new item is there
            var found = await repository.GetByIdAsync(expected.ID);
            Assert.AreEqual(expected, found);
        }

        [TestMethod]
        public async Task CreateInvalidModel()
        {
            // Given: Five items in the respository
            repository.AddItems(5);

            // And: The model is invalid
            controller.ModelState.AddModelError("error", "test");

            // When: Adding a new item
            var expected = MockBudgetTxRepository.MakeItem(6);
            var actionresult = await controller.Create(expected);

            // Then: Returns not found result
            var nfresult = Assert.That.IsOfType<NotFoundResult>(actionresult);
            Assert.AreEqual(404, nfresult.StatusCode);

            // And: No change to the repository
            Assert.AreEqual(5, repository.All.Count());
        }
        [TestMethod]
        public async Task CreateFailed()
        {
            // Given: Respository in failure state
            repository.Ok = false;

            // When: Adding a new item
            var expected = MockBudgetTxRepository.MakeItem(6);
            var actionresult = await controller.Create(expected);

            // Then: Returns status code 500 object result
            var nfresult = Assert.That.IsOfType<ObjectResult>(actionresult);
            Assert.AreEqual(500, nfresult.StatusCode);
        }

    }

    internal static class MyAssert
    {
        public static T IsOfType<T>(this Assert _, object actual) where T: class
        {
            if (actual is T)
                return actual as T;

            throw new AssertFailedException($"Assert.That.IsOfType failed. Expected <{typeof(T).Name}> Actual <{actual.GetType().Name}>");
        }

        public static void ActionResultOk(this Assert _, IActionResult actionresult)
        {
            var objectresult = actionresult as ObjectResult;
            if (objectresult?.StatusCode == 500)
                throw new AssertFailedException($"Assert.That.ActionResultOk failed <{objectresult.Value as string}>.");
        }
    }

    class MockBudgetTxRepository : IRepository<BudgetTx>
    {
        public void AddItems(int numitems) => Items.AddRange(Enumerable.Range(1, numitems).Select(MakeItem));

        static readonly DateTime defaulttimestamp = new DateTime(2020, 1, 1);

        public static BudgetTx MakeItem(int x) => new BudgetTx() { ID = x, Amount = x, Category = x.ToString(), Timestamp = defaulttimestamp };

        public bool Ok
        {
            get
            {
                if (!_Ok)
                    throw new Exception("Failed");
                return _Ok;
            }
            set
            {
                _Ok = value;
            }
        }
        public bool _Ok = true;

        public List<BudgetTx> Items { get; } = new List<BudgetTx>();

        public IQueryable<BudgetTx> All => Items.AsQueryable();

        public IQueryable<BudgetTx> OrderedQuery => throw new System.NotImplementedException();

        public Task AddAsync(BudgetTx item)
        {
            if (Ok)
                Items.Add(item);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<BudgetTx> items)
        {
            throw new System.NotImplementedException();
        }

        public Stream AsSpreadsheet()
        {
            throw new System.NotImplementedException();
        }

        public IQueryable<BudgetTx> ForQuery(string q) => string.IsNullOrEmpty(q) ? All : All.Where(x => x.Category.Contains(q));

        public Task<BudgetTx> GetByIdAsync(int? id) => Ok ? Task.FromResult(All.Single(x => x.ID == id.Value)) : Task.FromResult<BudgetTx>(null);

        public Task RemoveAsync(BudgetTx item)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> TestExistsByIdAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public Task UpdateAsync(BudgetTx item)
        {
            throw new System.NotImplementedException();
        }
    }
}
