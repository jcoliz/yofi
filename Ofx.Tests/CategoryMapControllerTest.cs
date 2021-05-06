using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;
using OfxWeb.Asp.Controllers;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

//
// This is my first controller unit test. I am going to start with an easy controller, the category map controller.
//

namespace Ofx.Tests
{

    /// <summary>
    /// This is a container for base test functionality that is common to most or all controllers
    /// </summary>
    /// <typeparam name="T">Type of object under test</typeparam>
    /// <typeparam name="C">Type of controller</typeparam>
    class ControllerTestHelper<T, C> where C : IController<T>
    {
        public C controller { set; get; } = default(C);

        public ApplicationDbContext context = null;

        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ApplicationDbContext")
                .Options;

            context = new ApplicationDbContext(options);

            //controller = new C();
        }

        public void Cleanup()
        {
            // Didn't actually solve anything. Keep it around for possible future problem
            //DetachAllEntities();

            // https://stackoverflow.com/questions/33490696/how-can-i-reset-an-ef7-inmemory-provider-between-unit-tests
            context?.Database.EnsureDeleted();
            context = null;
            controller = default(C);
        }
    }

    [TestClass]
    public class CategoryMapControllerTest
    {
        private ControllerTestHelper<CategoryMap, CategoryMapsController> helper = null;
        CategoryMapsController controller => helper?.controller;
        ApplicationDbContext context => helper?.context;

        [TestInitialize]
        public void SetUp()
        {
            helper = new ControllerTestHelper<CategoryMap, CategoryMapsController>();
            helper.SetUp();
            helper.controller = new CategoryMapsController(context);
        }

        [TestCleanup]
        public void Cleanup()
        {
            helper.Cleanup();
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
            var items = MakeFiveItems();
            context.AddRange(items);
            await context.SaveChangesAsync();

            return items;
        }

        private List<CategoryMap> MakeFiveItems()
        {
            var items = new List<CategoryMap>();
            items.Add(new CategoryMap() { Category = "B", SubCategory = "A", Key1 = "1", Key2 = "2", Key3 = "3" });
            items.Add(new CategoryMap() { Category = "A", SubCategory = "A", Key1 = "1", Key2 = "2", Key3 = "2" });
            items.Add(new CategoryMap() { Category = "C", SubCategory = "A", Key1 = "1", Key2 = "2", Key3 = "5" });
            items.Add(new CategoryMap() { Category = "A", SubCategory = "A", Key1 = "1", Key2 = "1", Key3 = "1" });
            items.Add(new CategoryMap() { Category = "B", SubCategory = "B", Key1 = "1", Key2 = "2", Key3 = "4" });

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
        [TestMethod]
        public async Task Download()
        {
            var items = await AddFiveItems();
            var result = await controller.Download();
            var fcresult = result as FileContentResult;
            var data = fcresult.FileContents;

            var incoming = new HashSet<CategoryMap>();
            using (var stream = new MemoryStream(data))
            {
                var excel = new ExcelPackage(stream);
                var worksheet = excel.Workbook.Worksheets.Where(x => x.Name == "CategoryMaps").Single();
                worksheet.ExtractInto(incoming);
            }

            Assert.AreEqual(5, incoming.Count);

            var expected = incoming.Where(x => x.Key3 == "2").Single();
            var actual = incoming.Where(x => x.Key3 == "2").Single();

            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public async Task Upload()
        {
            // Build a spreadsheet with items
            var items = MakeFiveItems();
            byte[] reportBytes;
            using (var package = new ExcelPackage())
            {
                var sheetname = $"{nameof(CategoryMap)}s";
                var worksheet = package.Workbook.Worksheets.Add(sheetname);
                worksheet.PopulateFrom(items, out _, out _);
                reportBytes = package.GetAsByteArray();
            }

            // Create a formfile with it
            var stream = new MemoryStream(reportBytes);
            IFormFile file = new FormFile(stream, 0, reportBytes.Length, $"{nameof(CategoryMap)}s", $"{nameof(CategoryMap)}s.xlsx");

            // Upload that
            var result = await controller.Upload(new List<IFormFile>() { file });

            // Test the status
            var actual = result as ViewResult;
            var model = actual.Model as IEnumerable<CategoryMap>;

            Assert.AreEqual(5, model.Count());
        }
        [TestMethod]
        public async Task UploadWithID()
        {
            // Start out with one item in the DB
            var initial = new CategoryMap() { Category = "A", SubCategory = "A", Key1 = "1", Key2 = "1", Key3 = "1" };
            context.Add(initial);
            await context.SaveChangesAsync();

            // One of the new items has an overlapping ID. What we expect is that the ID will get ignored and it will also get included
            // For ease of figuring out the results, we are going to use the same data as the item which will be returned FIRST
            // in the sorted view. Ergo the first and second of the later results should be identical, just with different IDs.
            var items = MakeFiveItems();
            items[0].ID = initial.ID;

            // Build a spreadsheet with items
            byte[] reportBytes;
            using (var package = new ExcelPackage())
            {
                var sheetname = $"{nameof(CategoryMap)}s";
                var worksheet = package.Workbook.Worksheets.Add(sheetname);
                worksheet.PopulateFrom(items, out _, out _);
                reportBytes = package.GetAsByteArray();
            }

            // Create a formfile with it
            var stream = new MemoryStream(reportBytes);
            IFormFile file = new FormFile(stream, 0, reportBytes.Length, $"{nameof(CategoryMap)}s", $"{nameof(CategoryMap)}s.xlsx");

            // Upload that
            var result = await controller.Upload(new List<IFormFile>() { file });

            // Test the status
            var actual = result as ViewResult;
            var model = actual.Model as IEnumerable<CategoryMap>;

            // Still, we only get 5 back because it is going to just give us a view of what we uploaded.
            Assert.AreEqual(5, model.Count());

            // So we need to dig into the database state to find the others.

            var sortedstate = context.CategoryMaps.OrderBy(x => x.Key3);

            // Now there should be SIX, cuz we started withe one and added five. 
            Assert.AreEqual(6, sortedstate.Count());

            // Further, the first two should be identical other than ID.
            var first = sortedstate.Take(1).Single();
            var second = sortedstate.Skip(1).Take(1).Single();

            Assert.AreEqual(first.Key3, second.Key3);
            Assert.AreNotEqual(first.ID, second.ID);
        }
    }
}