using Common.AspNet;
using Common.DotNet.Test;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Core.Repositories.Wire;
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
    public class BaseControllerSlimTest<T>: IFakeObjectsSaveTarget where T: class, IModelItem<T>, new()
    {
        #region Fields

        protected IController<T> controller;
        protected IRepository<T> repository;

        #endregion

        #region Helpers

        public void AddRange(System.Collections.IEnumerable objects)
        {
            if (objects is IEnumerable<T> items)
            {
                repository.AddRangeAsync(items).Wait();
            }
        }

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

        #endregion

        #region Tests

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
            var model = Assert.That.IsOfType<IWireQueryResult<T>>(viewresult.Model);

            // And: Model is empty
            Assert.AreEqual(0, model.Items.Count());
        }

        [TestMethod]
        public async Task DetailsFound()
        {
            // Given: Five items in the respository
            var data = FakeObjects<T>.Make(5).SaveTo(this);
            var selected = data.Last();

            // When: Retrieving details for a selected item
            var actionresult = await controller.Details(selected.ID);

            // Then: Returns a viewmodel with an expected item
            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);
            var model = Assert.That.IsOfType<T>(viewresult.Model);

            // And: The selected item is the one returned
            Assert.AreEqual(selected, model);
        }

        [TestMethod]
        public async Task CreateInitial()
        {
            // When: Calling Create
            var actionresult = await controller.Create();

            // Then: Returns a viewmodel 
            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);

            // And: It returns an empty model
            if (viewresult.Model is Transaction model)
            {
                Assert.IsNull(model.Payee);
                Assert.IsNull(model.Memo);
            }
            else
                Assert.IsNull(viewresult.Model);
        }

        [TestMethod]
        public async Task Create()
        {
            // Given: Five items in the respository, plus another ready to add
            var data = FakeObjects<T>.Make(5).SaveTo(this).Add(1);

            // When: Adding a new item
            var added = data.Group(1).Single();
            var actionresult = await controller.Create(added);

            // Then: Returns a redirection to Index
            var redirresult = Assert.That.IsOfType<RedirectToActionResult>(actionresult);
            Assert.AreEqual("Index", redirresult.ActionName);

            // And: All 6 items in the repository
            Assert.IsTrue(repository.All.SequenceEqual(data));

            // And: The new item is there
            var found = await repository.GetByIdAsync(added.ID);
            Assert.AreEqual(added, found);
        }

        [TestMethod]
        public async Task EditDetailsFound()
        {
            // Given: Five items in the respository
            var data = FakeObjects<T>.Make(5).SaveTo(this);
            var selected = data.Last();

            // When: Retrieving details for a selected item to edit it
            var actionresult = await controller.Edit(selected.ID);

            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);
            var model = Assert.That.IsOfType<T>(viewresult.Model);

            // Then: The selected item is the one returned
            Assert.AreEqual(selected, model);
        }

        [TestMethod]
        public async Task EditObjectValues()
        {
            // Given: Five items in the respository, plus another ready to use for editing
            var data = FakeObjects<T>.Make(5).SaveTo(this).Add(1);

            // When: Changing details for a selected item
            var selected = data.Group(0).Last();
            var newvalues = data.Group(1).Single();
            var id = newvalues.ID = selected.ID;
            var actionresult = await controller.Edit(id, newvalues);

            // Then: Returns a redirection to Index
            var redirresult = Assert.That.IsOfType<RedirectToActionResult>(actionresult);
            Assert.AreEqual("Index", redirresult.ActionName);

            // And: Item has been updated
            var actual = await repository.GetByIdAsync(id);
            Assert.AreEqual(newvalues, actual);
        }

        [TestMethod]
        public async Task DeleteDetailsFound()
        {
            // Given: Five items in the respository
            var data = FakeObjects<T>.Make(5).SaveTo(this);
            var selected = data.Last();

            // When: Retrieving details for a selected item
            var actionresult = await controller.Delete(selected.ID);

            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);
            var model = Assert.That.IsOfType<T>(viewresult.Model);

            // Then: The selected item is the one returned
            Assert.AreEqual(selected, model);
        }

        [TestMethod]
        public async Task DeleteConfirmed()
        {
            // Given: Many items in the respository
            var data = FakeObjects<T>.Make(5).Add(1).SaveTo(this);
            var remaining = data.Group(0);
            var deleted = data.Group(1).Single();

            // When: Deleting a selected item
            var actionresult = await controller.DeleteConfirmed(deleted.ID);

            // Then: Returns a redirection to Index
            var redirresult = Assert.That.IsOfType<RedirectToActionResult>(actionresult);
            Assert.AreEqual("Index", redirresult.ActionName);

            // And: There are now 4 items in the repository
            Assert.IsTrue(repository.All.SequenceEqual(remaining));

            // And: The selected item is not in the repository
            Assert.IsFalse(repository.All.Any(x => x.ID == deleted.ID));
        }

        [TestMethod]
        public async Task Download()
        {
            // Given: Many items in the respository
            var data = FakeObjects<T>.Make(25).SaveTo(this);

            // When: Downloading them as a spreadsheet
            var actionresult = await controller.Download();

            // Then: Returns a filestream result
            var fsresult = Assert.That.IsOfType<FileStreamResult>(actionresult);

            // And: Items in the filesteam match the repository
            var stream = fsresult.FileStream;
            using var ssr = new SpreadsheetReader();
            ssr.Open(stream);
            var actual = ssr.Deserialize<T>();
            Assert.IsTrue(repository.All.SequenceEqual(actual));
        }

        [TestMethod]
        public virtual async Task Upload()
        {
            // Given: A file with 10 new items
            var expected = FakeObjects<T>.Make(10).Group(0);
            var file = GivenAFileOf(expected);

            // When: Uploading that
            var actionresult = await controller.Upload(new List<IFormFile>() { file });

            // Then: View is returned
            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);

            // And: Correct kind of model is returned 
            var model = Assert.That.IsOfType<IEnumerable<T>>(viewresult.Model);

            // And: Model matches original items, sorted by default
            Assert.IsTrue(new T().InDefaultOrder(expected.AsQueryable()).SequenceEqual(model));

            // And: All the items are in the repository
            Assert.IsTrue(expected.SequenceEqual(repository.All));
        }

        #endregion
    }
}
