using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxWeb.Asp.Controllers;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

//
// This is my first controller unit test. I am going to start with an easy controller, the category map controller.
//

namespace Ofx.Tests
{
    [TestClass]
    public class CategoryMapControllerTest
    {
        CategoryMapsController controller = null;
        ApplicationDbContext context = null;

        [TestInitialize]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ApplicationDbContext")
                .Options;

            context = new ApplicationDbContext(options);

            controller = new CategoryMapsController(context);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Didn't actually solve anything. Keep it around for possible future problem
            //DetachAllEntities();

            // https://stackoverflow.com/questions/33490696/how-can-i-reset-an-ef7-inmemory-provider-between-unit-tests
            context?.Database.EnsureDeleted();
            context = null;
            controller = null;
        }

        private void DetachAllEntities()
        {
            var changedEntriesCopy = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in changedEntriesCopy)
                entry.State = EntityState.Detached;
        }

        private async Task<List<CategoryMap>> AddFiveItems()
        {
            var items = new List<CategoryMap>();
            items.Add(new CategoryMap() { Category = "B", SubCategory = "A", Key1 = "1", Key2 = "2", Key3 = "3" });
            items.Add(new CategoryMap() { Category = "A", SubCategory = "A", Key1 = "1", Key2 = "2", Key3 = "2" });
            items.Add(new CategoryMap() { Category = "C", SubCategory = "A", Key1 = "1", Key2 = "2", Key3 = "5" });
            items.Add(new CategoryMap() { Category = "A", SubCategory = "A", Key1 = "1", Key2 = "1", Key3 = "1" });
            items.Add(new CategoryMap() { Category = "B", SubCategory = "B", Key1 = "1", Key2 = "2", Key3 = "4" });
            context.AddRange(items);
            await context.SaveChangesAsync();

            return items;
        }

        [TestMethod]
        public void Null()
        {
            var tested = new CategoryMapsController(null);

            Assert.IsNotNull(tested);
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public async Task IndexEmpty()
        {
            var result = await controller.Index();
            var actual = result as ViewResult;
            var model = actual.Model as List<CategoryMap>;

            Assert.AreEqual(0, model.Count);
        }

        [TestMethod]
        public async Task IndexSingle()
        {
            var expected = new CategoryMap() { Category = "Testing", Key1 = "123" };

            context.Add(expected);
            await context.SaveChangesAsync();

            var result = await controller.Index();
            var actual = result as ViewResult;
            var model = actual.Model as List<CategoryMap>;

            Assert.AreEqual(1, model.Count);
            Assert.AreEqual(expected.Category, model[0].Category);
            Assert.AreEqual(expected.Key1, model[0].Key1);
        }
        [TestMethod]
        public async Task IndexMany()
        {
            await AddFiveItems();
            var result = await controller.Index();
            var actual = result as ViewResult;
            var model = actual.Model as List<CategoryMap>;

            Assert.AreEqual(5, model.Count);

            // Test the sort order. Key3 (sneakily!) contains the expected sort order.
            Assert.AreEqual("1", model[0].Key3);
            Assert.AreEqual("2", model[1].Key3);
            Assert.AreEqual("3", model[2].Key3);
            Assert.AreEqual("4", model[3].Key3);
            Assert.AreEqual("5", model[4].Key3);
        }
        [TestMethod]
        public async Task DetailsFound()
        {
            var items = await AddFiveItems();
            var expected = items[3];
            var result = await controller.Details(expected.ID);
            var actual = result as ViewResult;
            var model = actual.Model as CategoryMap;

            Assert.AreEqual(expected.Key3, model.Key3);
        }
        [TestMethod]
        public async Task DetailsNotFound()
        {
            context.Add(new CategoryMap() { Category = "B", SubCategory = "A", Key1 = "1", Key2 = "2", Key3 = "3" }); ;
            await context.SaveChangesAsync();

            var result = await controller.Details(3);
            var actual = result as NotFoundResult;

            Assert.AreEqual(404, actual.StatusCode);
        }

        [TestMethod]
        public async Task EditFound()
        {
            var items = await AddFiveItems();
            var expected = items[3];
            var result = await controller.Edit(expected.ID);
            var actual = result as ViewResult;
            var model = actual.Model as CategoryMap;

            Assert.AreEqual(expected.Key3, model.Key3);
        }
        [TestMethod]
        public async Task EditNotFound()
        {
            context.Add(new CategoryMap() { Category = "B", SubCategory = "A", Key1 = "1", Key2 = "2", Key3 = "3" }); ;
            await context.SaveChangesAsync();

            var result = await controller.Edit(3);
            var actual = result as NotFoundResult;

            Assert.AreEqual(404, actual.StatusCode);
        }

        [TestMethod]
        public async Task Create()
        {
            context.Add(new CategoryMap() { Category = "B", SubCategory = "A", Key1 = "1", Key2 = "2", Key3 = "3" }); ;
            await context.SaveChangesAsync();

            var expected = new CategoryMap() { Category = "A", SubCategory = "A", Key1 = "1", Key2 = "2", Key3 = "2" };
            var result = await controller.Create(expected);
            var actual = result as RedirectToActionResult;

            Assert.AreEqual("Index", actual.ActionName);

            Assert.AreEqual(2, expected.ID);

            var count = await context.CategoryMaps.CountAsync();

            Assert.AreEqual(2, count);
        }
        [TestMethod]
        public async Task EditObjectValues()
        {
            var initial = new CategoryMap() { Category = "B", SubCategory = "A", Key1 = "1", Key2 = "2", Key3 = "3" };
            context.Add(initial);
            await context.SaveChangesAsync();
            var id = initial.ID;

            // Need to detach the entity we originally created, to set up the same state the controller would be
            // in with not already haveing a tracked object.
            context.Entry(initial).State = EntityState.Detached;

            var updated = new CategoryMap() { Category = "A", SubCategory = "A", Key1 = "1", Key2 = "2", Key3 = "2", ID = id };
            var result = await controller.Edit(id,updated);
            var actual = result as RedirectToActionResult;

            Assert.AreEqual("Index", actual.ActionName);
        }
        [TestMethod]
        public async Task DeleteFound()
        {
            var items = await AddFiveItems();
            var expected = items[3];
            var result = await controller.Delete(expected.ID);
            var actual = result as ViewResult;
            var model = actual.Model as CategoryMap;

            Assert.AreEqual(expected.Key3, model.Key3);
        }
        [TestMethod]
        public async Task DeleteConfirmed()
        {
            var items = await AddFiveItems();
            var expected = items[3];
            var result = await controller.DeleteConfirmed(expected.ID);
            var actual = result as RedirectToActionResult;

            Assert.AreEqual("Index", actual.ActionName);

            var count = await context.CategoryMaps.CountAsync();

            Assert.AreEqual(4, count);
        }
    }
}