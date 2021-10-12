﻿using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Tests.Helpers;
using YoFi.AspNet.Data;
using YoFi.AspNet.Models;

namespace Common.AspNet.Test
{
    /// <summary>
    /// This is a container for base test functionality that is common to most or all controllers
    /// </summary>
    /// 
    /// <remarks>
    /// Steps to use these tests:
    ///     1. Make your controller inherit from IController(ModelType). Handle any missing interface members.
    ///     2. Make your model inherit from IID
    ///     3. Implement Equals/GetHash on your Model, Ensure to exclude ID from equality.
    ///     4. Create new controllertest,  use PayeeControllerTest as an example. 
    ///     5. Add FIVE sample items to helper items. Be sure to order the sort key in the order that Index will return thenm
    ///     6. Updaate the sort key function
    ///     7. Create tests for any situations not covered by the base tests.
    /// 
    /// </remarks>
    /// 
    /// <typeparam name="T">Type of object under test</typeparam>
    /// <typeparam name="C">Type of controller</typeparam>
    class ControllerTestHelper<T, C> where C : IController<T> where T : class, IID, new()
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

        /*
        ILoggerFactory log = LoggerFactory.Create(builder =>
        {
            builder.AddConsole(x => { });
            builder.AddFilter((cat, level) => cat == DbLoggerCategory.Database.Command.Name && level == LogLevel.Information);  // "Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
        });
        */

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
            DetachAllEntities();

            // https://stackoverflow.com/questions/33490696/how-can-i-reset-an-ef7-inmemory-provider-between-unit-tests
            context?.Database.EnsureDeleted();
            context = null;
            controller = default(C);
        }

        private void DetachAllEntities()
        {
            foreach (var entry in context.ChangeTracker.Entries())
                context.Entry(entry.Entity).State = EntityState.Detached;
        }

        public async Task AddFiveItems()
        {
            // context.AddRange(Items) doesn't work :(
            foreach (var item in Items)
                context.Add(item);

            await context.SaveChangesAsync();
        }

        public HashSet<TExtract> ExtractFromSpreadsheet<TExtract>(Stream stream) where TExtract : class, new()
        {
            var incoming = new HashSet<TExtract>();
            using (var ssr = new SpreadsheetReader())
            {
                ssr.Open(stream);
                var items = ssr.Deserialize<TExtract>();
                incoming.UnionWith(items);
            }

            return incoming;
        }

        public void Empty()
        {
            Assert.IsNotNull(controller);
        }

        public async Task IndexEmpty() => await IndexEmpty<T>();

        public async Task IndexEmpty<TSpecific>()
        {
            var result = await controller.Index();
            var actual = result as ViewResult;
            var model = actual.Model as IEnumerable<TSpecific>;

            Assert.AreEqual(0, model.Count());
        }

        public async Task IndexSingle()
        {
            var expected = Items[0];

            context.Add(expected);
            await context.SaveChangesAsync();

            var result = await controller.Index();
            var actual = result as ViewResult;
            var model = actual.Model as IEnumerable<T>;

            Assert.AreEqual(1, model.Count());
            Assert.IsTrue(model.Single().Equals(expected));
        }

        public async Task IndexMany(bool additems = true)
        {
            if (additems)
                await AddFiveItems();
            var result = await controller.Index();
            var actual = result as ViewResult;
            var model = actual.Model as List<T>;

            // Sort the original items by Key
            Items.Sort((x, y) => KeyFor(x).CompareTo(KeyFor(y)));

            // Test that the resulting items are in the same order
            Assert.IsTrue(model.SequenceEqual(Items));
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

            var maxid = dbset.Max(x => x.ID);
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
            var model = actual.Model as T;

            Assert.AreEqual(expected, model);
        }
        public async Task EditNotFound()
        {
            context.Add(Items[0]);
            await context.SaveChangesAsync();

            var maxid = dbset.Max(x => x.ID);
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
            var redir = result as RedirectToActionResult;

            Assert.AreEqual("Index", redir.ActionName);

            var count = await dbset.CountAsync();
            Assert.AreEqual(2, count);

            // Single() will fail if there's a problem.
            var actual1 = dbset.AsEnumerable().Where(x => KeyFor(x) == KeyFor(Items[1])).Single();
            var actual2 = dbset.AsEnumerable().Where(x => KeyFor(x) == KeyFor(Items[2])).Single();
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
            var model = actual.Model as T;

            Assert.AreEqual(expected, model);
        }
        public async Task DeleteConfirmed(string actionname = "Index")
        {
            await AddFiveItems();
            var expected = Items[3];
            var result = await controller.DeleteConfirmed(expected.ID);
            var actual = result as RedirectToActionResult;

            Assert.AreEqual(actionname, actual.ActionName);

            var count = await dbset.CountAsync();

            Assert.AreEqual(4, count);
        }
        public async Task<HashSet<T>> Download()
        {
            await AddFiveItems();
            var result = await controller.Download();

            HashSet<T> incoming = null;
            Assert.IsTrue(result is FileStreamResult);

            var fcresult = result as FileStreamResult;
            var stream = fcresult.FileStream;

            incoming = ExtractFromSpreadsheet<T>(stream);

            Assert.AreEqual(5, incoming.Count);

            // pick an arbitrary item
            var expected = Items[4];

            // Find the matching item in the incoming
            var actual = incoming.Where(x => KeyFor(x) == KeyFor(expected)).Single();

            // And they should be the same
            Assert.AreEqual(expected, actual);

            return incoming;
        }

#if true
        static public IFormFile PrepareUpload<X>(ICollection<X> what) where X : class
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
            var filename = $"{typeof(T).Name}s";
            stream.Seek(0, SeekOrigin.Begin);
            IFormFile file = new FormFile(stream, 0, stream.Length, filename, $"{filename}.xlsx");

            return file;
        }
#else
        static public IFormFile PrepareUpload<X>(ICollection<X> what) where X: class
        {
            // Build a spreadsheet with the chosen number of items
            var stream = new MemoryStream();
            var sheetname = $"{typeof(T).Name}s";
            SpreadsheetHelpers.InsertItems(stream, what, sheetname);

            stream.Seek(0, SeekOrigin.Begin);
            using (var outstream = File.OpenWrite($"{sheetname}.xlsx"))
            {
                Console.WriteLine($"Writing {sheetname}...");
                stream.CopyTo(outstream);
            }

            // Create a formfile with it
            // Note that we are not disposing the stream. User of the file will do so later.
            stream.Seek(0,SeekOrigin.Begin);
            IFormFile file = new FormFile(stream, 0, stream.Length, sheetname, $"{sheetname}.xlsx");

            return file;
        }
#endif

        public async Task<IActionResult> DoUpload(ICollection<T> what)
        {
            // Make an HTML Form file containg a spreadsheet.
            var file = PrepareUpload(what);

            // Upload that
            var result = await controller.Upload(new List<IFormFile>() { file });

            return result;
        }

        /// <summary>
        /// Test uploading a file
        /// </summary>
        /// <param name="duplicates">How many of the uploaded items were really duplicates, so we shouldn't expect to see them back</param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> Upload(int numitems = 5, int expect = 5)
        {
            var result = await DoUpload(Items.Take(numitems).ToList());

            // Test the status
            var actual = result as ViewResult;
            var model = actual.Model as IEnumerable<T>;

            Assert.AreEqual(expect, model.Count());

            return model;
        }

        public async Task UploadEmpty()
        {
            // When: Uploading an empty set for imported transactions
            var result = await controller.Upload(new List<IFormFile>());

            // Then: The the operation fails
            Assert.IsTrue(result is BadRequestObjectResult);
        }

        public async Task UploadWithID()
        {
            // Start out with one item in the DB. We are picking the ONE item that Upload doesn't upload.
            var expected = Items[4];
            context.Add(expected);
            await context.SaveChangesAsync();

            // One of the new items has an overlapping ID, but will be different in every way. We expect that
            // The end result is that the database will == items
            Items[0].ID = expected.ID;

            // Just upload the first four items. The fifth, we already did above
            await Upload(4, 4);

            // From here we can just use the Index test, but not add items. There should be the proper "items"
            // all there now.

            await IndexMany(false);

#if false
            // TODO: Write a different test that uses this.

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
#endif
        }
        public async Task UploadDuplicate()
        {
            // Start out with one item in the DB. We are picking the ONE item that Upload doesn't upload.
            var initial = Items[0];
            context.Add(initial);
            await context.SaveChangesAsync();

            // Need to detach the entity we originally created, to set up the same state the controller would be
            // in with not already haveing a tracked object.
            context.Entry(initial).State = EntityState.Detached;

            Items[0].ID = 0;

            // Now upload all the items. What should happen here is that only items 1-4 (not 0) get
            // uploaded, because item 0 is already there, so it gets removed as a duplicate.
            var actual = await Upload(5, 4);

            // Let's make sure that Item[0] indeed did not get updated.
            var findinitial = actual.Where(x => x == Items[0]);

            Assert.AreEqual(0, findinitial.Count());
        }
    }
}
