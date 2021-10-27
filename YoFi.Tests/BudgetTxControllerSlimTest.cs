using Common.AspNet;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Controllers;
using YoFi.AspNet.Models;
using YoFi.Tests.Helpers;

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

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task DetailsFound(bool ok)
        {
            // Given: Five items in the respository
            repository.AddItems(5);

            // And: Repository in the given state
            repository.Ok = ok;

            // When: Retrieving details for a selected item
            var selected = repository.All.Skip(1).First();
            var actionresult = await controller.Details(selected.ID);
            ThenSucceedsOrFailsAsExpected(actionresult, ok);
            if (!ok) return;

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

        [TestMethod]
        public async Task EditDetailsFound()
        {
            // Given: Five items in the respository
            repository.AddItems(5);

            // When: Retrieving details for a selected item to edit it
            var selected = repository.All.Skip(1).First();
            var actionresult = await controller.Edit(selected.ID);
            Assert.That.ActionResultOk(actionresult);
            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);
            var model = Assert.That.IsOfType<BudgetTx>(viewresult.Model);

            // Then: The selected item is the one returned
            Assert.AreEqual(selected, model);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task EditObjectValues(bool ok)
        {
            // Given: Five items in the respository
            repository.AddItems(5);

            // And: Repository in the given state
            repository.Ok = ok;

            // When: Changing details for a selected item
            var selected = repository.All.Skip(1).First();
            var expected = MockBudgetTxRepository.MakeItem(6);
            var id = expected.ID = selected.ID;
            var actionresult = await controller.Edit(id, expected);
            ThenSucceedsOrFailsAsExpected(actionresult, ok);
            if (!ok) return;

            // Then: Returns a redirection to Index
            var redirresult = Assert.That.IsOfType<RedirectToActionResult>(actionresult);
            Assert.AreEqual("Index", redirresult.ActionName);

            // And: Item has been updated
            var actual = await repository.GetByIdAsync(id);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task EditObjectValuesModelInvalid()
        {
            // Given: Five items in the respository
            repository.AddItems(5);

            // And: The model is invalid
            controller.ModelState.AddModelError("error", "test");

            // When: Changing details for a selected item
            var selected = repository.All.Skip(1).First();
            var expected = MockBudgetTxRepository.MakeItem(6);
            var id = expected.ID = selected.ID;
            var actionresult = await controller.Edit(id, expected);

            // Then: Returns not found result
            var nfresult = Assert.That.IsOfType<NotFoundResult>(actionresult);
            Assert.AreEqual(404, nfresult.StatusCode);

            // And: Item has not been updated
            var actual = await repository.GetByIdAsync(id);
            Assert.AreNotEqual(expected, actual);
        }

        [TestMethod]
        public async Task EditObjectValuesDontMatch()
        {
            // Given: Five items in the respository
            repository.AddItems(5);

            // When: Changing details for a selected item
            var selected = repository.All.Skip(1).First();
            var expected = MockBudgetTxRepository.MakeItem(6);
            var id = expected.ID = selected.ID;
            var badid = id + 1;
            var actionresult = await controller.Edit(badid, expected);
            Assert.That.ActionResultOk(actionresult);

            // Then: Returns bad request
            Assert.That.IsOfType<BadRequestResult>(actionresult);

            // And: Item has NOT been updated
            var actual = await repository.GetByIdAsync(id);
            var actualbadid = await repository.GetByIdAsync(badid);
            Assert.AreEqual(selected, actual);
            Assert.AreNotEqual(expected, actual);
            Assert.AreNotEqual(expected, actualbadid);
        }

        [TestMethod]
        public async Task DeleteDetailsFound()
        {
            // Given: Five items in the respository
            repository.AddItems(5);

            // When: Retrieving details for a selected item
            var selected = repository.All.Skip(1).First();
            var actionresult = await controller.Delete(selected.ID);
            Assert.That.ActionResultOk(actionresult);
            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);
            var model = Assert.That.IsOfType<BudgetTx>(viewresult.Model);

            // Then: The selected item is the one returned
            Assert.AreEqual(selected, model);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task DeleteConfirmed(bool ok)
        {
            // Given: Five items in the respository
            repository.AddItems(5);

            // And: Repository in the given state
            repository.Ok = ok;

            // When: Deleting a selected item
            var selected = repository.All.Skip(1).First();
            var actionresult = await controller.DeleteConfirmed(selected.ID);
            ThenSucceedsOrFailsAsExpected(actionresult, ok);
            if (!ok) return;

            // Then: Returns a redirection to Index
            var redirresult = Assert.That.IsOfType<RedirectToActionResult>(actionresult);
            Assert.AreEqual("Index", redirresult.ActionName);

            // And: There are now 4 items in the repository
            Assert.AreEqual(4, repository.All.Count());

            // And: The selected item is not in the repository
            Assert.IsFalse(repository.All.Any(x => x.ID == selected.ID));
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task Download(bool ok)
        {
            // Given: 20 items in the respository
            repository.AddItems(20);

            // And: Repository in the given state
            repository.Ok = ok;

            // When: Downloading them as a spreadsheet
            var actionresult = await controller.Download();
            ThenSucceedsOrFailsAsExpected(actionresult, ok);
            if (!ok) return;

            // Then: Returns a filestream result
            var fsresult = Assert.That.IsOfType<FileStreamResult>(actionresult);

            // And: Items in the filesteam match the repository
            var stream = fsresult.FileStream;
            using var ssr = new SpreadsheetReader();
            ssr.Open(stream);
            var actual = ssr.Deserialize<BudgetTx>();
            Assert.IsTrue(repository.All.SequenceEqual(actual));
        }

        static public IFormFile GivenAFileOf<X>(ICollection<X> what) where X : class
        {
            // Build a spreadsheet with the chosen number of items
            // Note that we are not disposing the stream. User of the file will do so later.
            var stream = new MemoryStream();
            using (var ssr = new SpreadsheetWriter())
            {
                ssr.Open(stream);
                ssr.Serialize(what);
            }

            // Create a formfile with it
            var filename = $"{typeof(X).Name}s";
            stream.Seek(0, SeekOrigin.Begin);
            IFormFile file = new FormFile(stream, 0, stream.Length, filename, $"{filename}.xlsx");

            return file;
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task Upload(bool ok)
        {
            // Given: A file with 10 new items
            var expected = MockBudgetTxRepository.MakeItems(10).ToList();
            var file = GivenAFileOf(expected);

            // And: Repository in the given state
            repository.Ok = ok;

            // When: Uploading that
            var actionresult = await controller.Upload(new List<IFormFile>() { file });
            ThenSucceedsOrFailsAsExpected(actionresult, ok);
            if (!ok) return;

            // Then: View is returned
            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);

            // And: Correct kind of model is returned 
            var model = Assert.That.IsOfType<IEnumerable<BudgetTx>>(viewresult.Model);

            // And: Model matches original items, sorted by category
            Assert.IsTrue(expected.OrderBy(x=>x.Category).SequenceEqual(model));

            // And: All the items are in the repository
            Assert.IsTrue(expected.SequenceEqual(repository.All));
        }

        [TestMethod]
        public async Task UploadEmptyFails()
        {
            // When: Uploading an empty list
            var actionresult = await controller.Upload(new List<IFormFile>());

            // Then: Returns bad request object result
            Assert.That.IsOfType<BadRequestObjectResult>(actionresult);
        }

        [TestMethod]
        public async Task UploadNullFails()
        {
            // When: Uploading null paramter
            var actionresult = await controller.Upload(null);

            // Then: Returns bad request object result
            Assert.That.IsOfType<BadRequestObjectResult>(actionresult);
        }

        void ThenSucceedsOrFailsAsExpected(IActionResult actionresult, bool ok)
        {
            // Then: Fails if expected
            if (!ok)
            {
                // Then: Returns status code 500 object result
                var nfresult = Assert.That.IsOfType<ObjectResult>(actionresult);
                Assert.AreEqual(500, nfresult.StatusCode);
                return;
            }

            // Otherwise: Result is OK, if expected
            Assert.That.ActionResultOk(actionresult);
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
}
