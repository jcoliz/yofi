using Common.AspNet;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Tests.Common;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Controllers.Slim
{
    /// <summary>
    /// Tests functionality that's common to all (or most) controllers, as represented by the
    /// IController(T) interface
    /// </summary>
    /// <remarks>
    /// Functionality specific to a certain controller can be tested in the inherited class
    /// </remarks>
    /// <typeparam name="T">Underlying Model type</typeparam>
    public class BaseControllerSlimTest<T> where T: class, IModelItem<T>, new()
    {
        protected IController<T> controller;
        protected IMockRepository<T> repository;

        /// <summary>
        /// Create a test file of a spreadsheet of <typeparamref name="X"/> items
        /// </summary>
        /// <typeparam name="X">Type of item to create</typeparam>
        /// <param name="what">Collection of those items</param>
        /// <returns>Form file as if used had uploaded it</returns>
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

        protected void ThenSucceedsOrFailsAsExpected(IActionResult actionresult, bool ok)
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
            var model = Assert.That.IsOfType<IEnumerable<T>>(viewresult.Model);

            // And: Model is empty
            Assert.AreEqual(0, model.Count());
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
            var model = Assert.That.IsOfType<T>(viewresult.Model);

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
        public async Task CreateInitial()
        {
            // When: Calling Create
            var actionresult = await controller.Create();
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
            var expected = repository.MakeItem(6);
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
            controller.SetErrorState();

            // When: Adding a new item
            var expected = repository.MakeItem(6);
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
            var expected = repository.MakeItem(6);
            var actionresult = await controller.Create(expected);

            // Then: Returns status code 500 object result
            var nfresult = Assert.That.IsOfType<ObjectResult>(actionresult);
            Assert.AreEqual(500, nfresult.StatusCode);
        }


        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task EditDetailsFound(bool ok)
        {
            // Given: Five items in the respository
            repository.AddItems(5);

            // And: Repository in the given state
            repository.Ok = ok;

            // When: Retrieving details for a selected item to edit it
            var selected = repository.All.Skip(1).First();
            var actionresult = await controller.Edit(selected.ID);
            ThenSucceedsOrFailsAsExpected(actionresult, ok);
            if (!ok) return;

            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);
            var model = Assert.That.IsOfType<T>(viewresult.Model);

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
            var expected = repository.MakeItem(6);
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
            controller.SetErrorState();

            // When: Changing details for a selected item
            var selected = repository.All.Skip(1).First();
            var expected = repository.MakeItem(6);
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
            var expected = repository.MakeItem(6);
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
            var model = Assert.That.IsOfType<T>(viewresult.Model);

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
            var actual = ssr.Deserialize<T>();
            Assert.IsTrue(repository.All.SequenceEqual(actual));
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task Upload(bool ok)
        {
            // Given: A file with 10 new items
            var expected = repository.MakeItems(10).ToList();
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
            var model = Assert.That.IsOfType<IEnumerable<T>>(viewresult.Model);

            // And: Model matches original items, sorted by default
            Assert.IsTrue(new T().InDefaultOrder(expected.AsQueryable()).SequenceEqual(model));

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
    }
}
