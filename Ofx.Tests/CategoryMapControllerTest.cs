using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;
using OfxWeb.Asp.Controllers;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System;
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
    class ControllerTestHelper<T, C> where C : IController<T> where T : class, IModelObject, new()
    {
        public C controller { set; get; } = default(C);

        public ApplicationDbContext context = null;

        /// <summary>
        /// List of test items which we will use for our test. Needs to be supplied by the user
        /// becasue only use knows how Items should look
        /// </summary>
        public List<T> Items;

        /// <summary>
        /// Function to return a sorting key for a given T object.
        /// </summary>
        public Func<T, string> KeyFor { private get; set; }

        /// <summary>
        /// Where in the context should we look for T items.
        /// </summary>
        public DbSet<T> dbset;

        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ApplicationDbContext")
                .Options;

            context = new ApplicationDbContext(options);

            //controller = new C();

            Items = new List<T>();
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

        private async Task AddFiveItems()
        {
            // context.AddRange(Items) doesn't work :(
            foreach (var item in Items)
                context.Add(item);

            await context.SaveChangesAsync();
        }

        public async Task IndexEmpty()
        {
            var result = await controller.Index();
            var actual = result as ViewResult;
            var model = actual.Model as List<T>;

            Assert.AreEqual(0, model.Count);
        }

        public async Task IndexSingle()
        {
            var expected = Items[0];

            context.Add(expected);
            await context.SaveChangesAsync();

            var result = await controller.Index();
            var actual = result as ViewResult;
            var model = actual.Model as List<T>;

            Assert.AreEqual(1, model.Count);
            Assert.AreEqual(expected, model.Single());
        }

        public async Task IndexMany()
        {
            await AddFiveItems();
            var result = await controller.Index();
            var actual = result as ViewResult;
            var model = actual.Model as List<T>;

            Assert.AreEqual(5, model.Count);

            // Sort the original items by Key
            Items.Sort((x,y) => KeyFor(x).CompareTo(KeyFor(y)));

            // Test that the resulting items are in the same order
            for (int i = 0; i < 5; ++i)
                Assert.AreEqual(Items[i], model[i]);
        }

        public async Task DetailsFound()
        {
            await AddFiveItems();
            var expected = Items[3];
            var result = await controller.Details(expected.ID);
            var actual = result as ViewResult;
            var model = actual.Model as T;

            Assert.AreEqual(expected, model);
        }

        public async Task DetailsNotFound()
        {
            context.Add(Items[0]);
            await context.SaveChangesAsync();

            var maxid = context.CategoryMaps.Max(x => x.ID);
            var badid = maxid + 1;

            var result = await controller.Details(badid);
            var actual = result as NotFoundResult;

            Assert.AreEqual(404, actual.StatusCode);
        }
        public async Task EditFound()
        {
            await AddFiveItems();
            var expected = Items[3];
            var result = await controller.Edit(expected.ID);
            var actual = result as ViewResult;
            var model = actual.Model as CategoryMap;

            Assert.AreEqual(expected, model);
        }
        public async Task EditNotFound()
        {
            context.Add(Items[0]);
            await context.SaveChangesAsync();

            var maxid = context.CategoryMaps.Max(x => x.ID);
            var badid = maxid + 1;

            var result = await controller.Edit(badid);
            var actual = result as NotFoundResult;

            Assert.AreEqual(404, actual.StatusCode);
        }
        public async Task Create()
        {
            context.Add(Items[1]);
            await context.SaveChangesAsync();

            var expected = Items[2];
            var result = await controller.Create(expected);
            var actual = result as RedirectToActionResult;

            Assert.AreEqual("Index", actual.ActionName);

            Assert.AreEqual(2, expected.ID);

            var count = await context.CategoryMaps.CountAsync();

            Assert.AreEqual(2, count);
        }
        public async Task EditObjectValues()
        {
            var initial = Items[3];
            context.Add(initial);
            await context.SaveChangesAsync();
            var id = initial.ID;

            // Need to detach the entity we originally created, to set up the same state the controller would be
            // in with not already haveing a tracked object.
            context.Entry(initial).State = EntityState.Detached;

            var updated = Items[1];
            updated.ID = id;
            var result = await controller.Edit(id, updated);
            var actual = result as RedirectToActionResult;

            Assert.AreEqual("Index", actual.ActionName);
        }

        public async Task DeleteFound()
        {
            await AddFiveItems();
            var expected = Items[3];
            var result = await controller.Delete(expected.ID);
            var actual = result as ViewResult;
            var model = actual.Model as CategoryMap;

            Assert.AreEqual(expected, model);
        }
        public async Task DeleteConfirmed()
        {
            await AddFiveItems();
            var expected = Items[3];
            var result = await controller.DeleteConfirmed(expected.ID);
            var actual = result as RedirectToActionResult;

            Assert.AreEqual("Index", actual.ActionName);

            var count = await context.CategoryMaps.CountAsync();

            Assert.AreEqual(4, count);
        }
        public async Task<HashSet<T>> Download()
        {
            await AddFiveItems();
            var result = await controller.Download();
            var fcresult = result as FileContentResult;
            var data = fcresult.FileContents;

            var incoming = new HashSet<T>();
            using (var stream = new MemoryStream(data))
            {
                var excel = new ExcelPackage(stream);
                var sheetname = $"{typeof(T).Name}s";
                var worksheet = excel.Workbook.Worksheets.Where(x => x.Name == sheetname).Single();
                worksheet.ExtractInto(incoming);
            }

            Assert.AreEqual(5, incoming.Count);

            // pick an arbitrary item
            var expected = Items[4];

            // Find the matching item in the incoming
            var actual = incoming.Where(x => KeyFor(x) == KeyFor(expected)).Single();

            // And they should be the same
            Assert.AreEqual(expected, actual);

            return incoming;
        }
        public async Task Upload()
        {
            // Build a spreadsheet with items
            byte[] reportBytes;
            var sheetname = $"{typeof(T).Name}s";
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(sheetname);
                worksheet.PopulateFrom(Items, out _, out _);
                reportBytes = package.GetAsByteArray();
            }

            // Create a formfile with it
            var stream = new MemoryStream(reportBytes);
            IFormFile file = new FormFile(stream, 0, reportBytes.Length, sheetname, $"{sheetname}.xlsx");

            // Upload that
            var result = await controller.Upload(new List<IFormFile>() { file });

            // Test the status
            var actual = result as ViewResult;
            var model = actual.Model as IEnumerable<T>;

            Assert.AreEqual(5, model.Count());
        }
        public async Task UploadWithID()
        {
            // Start out with one item in the DB
            var expected = Items[0];
            IModelObject imo = expected;
            context.Add(expected);
            await context.SaveChangesAsync();

            // One of the new items has an overlapping ID. What we expect is that the ID will get ignored and it will also get included
            // For ease of figuring out the results, we are going to use the same data as the item which will be returned FIRST
            // in the sorted view. Ergo the first and second of the later results should be identical, just with different IDs.

            await Upload();

            // So we need to dig into the database state to find the others.

            // There should be a single duplicate in key3 for items[0].key3
            var lookup = dbset.ToLookup(x => KeyFor(x), x => x);

            // tThese items should be identical other than ID.
            var first = lookup[KeyFor(expected)].Take(1).Single();
            var second = lookup[KeyFor(expected)].Skip(1).Take(1).Single();

            Assert.AreEqual(2, lookup[KeyFor(expected)].Count());
            Assert.AreEqual(expected, first);
            Assert.AreEqual(expected, second);
            Assert.AreNotEqual(first.ID, second.ID);
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

            helper.Items.Add(new CategoryMap() { Category = "B", SubCategory = "A", Key1 = "1", Key2 = "2", Key3 = "3" });
            helper.Items.Add(new CategoryMap() { Category = "A", SubCategory = "A", Key1 = "1", Key2 = "2", Key3 = "2" });
            helper.Items.Add(new CategoryMap() { Category = "C", SubCategory = "A", Key1 = "1", Key2 = "2", Key3 = "5" });
            helper.Items.Add(new CategoryMap() { Category = "A", SubCategory = "A", Key1 = "1", Key2 = "1", Key3 = "1" });
            helper.Items.Add(new CategoryMap() { Category = "B", SubCategory = "B", Key1 = "1", Key2 = "2", Key3 = "4" });

            helper.dbset = context.CategoryMaps;

            // Sample data items will use 'key3' as a unique sort idenfitier
            helper.KeyFor = (x => x.Key3);
        }

        [TestCleanup]
        public void Cleanup()
        {
            helper.Cleanup();
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
            return helper.Items;
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
        public async Task IndexEmpty() => await helper.IndexEmpty();
        [TestMethod]
        public async Task IndexSingle() => await helper.IndexSingle();
        [TestMethod]
        public async Task IndexMany() => await helper.IndexMany();
        [TestMethod]
        public async Task DetailsFound() => await helper.DetailsFound();
        [TestMethod]
        public async Task DetailsNotFound() => await helper.DetailsNotFound();
        [TestMethod]
        public async Task EditFound() => await helper.EditFound();
        [TestMethod]
        public async Task EditNotFound() => await helper.EditNotFound();
        [TestMethod]
        public async Task Create() => await helper.Create();
        [TestMethod]
        public async Task EditObjectValues() => await helper.EditObjectValues();
        [TestMethod]
        public async Task DeleteFound() => await helper.DeleteFound();
        [TestMethod]
        public async Task DeleteConfirmed() => await helper.DeleteConfirmed();
        [TestMethod]
        public async Task Download() => await helper.Download();
        [TestMethod]
        public async Task Upload() => await helper.Upload();
        [TestMethod]
        public async Task UploadWithID() => await helper.UploadWithID();
    }
}