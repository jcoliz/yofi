using Common.DotNet.Test;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Controllers;
using YoFi.AspNet.Data;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.Tests.Database
{
    [TestClass]
    public class AjaxPayeeControllerTest
    {
        private AjaxPayeeController controller;
        private IPayeeRepository repository;
        private ApplicationDbContext context;

        async Task AddFive()
        {
            await repository.AddRangeAsync
            (
                new List<Payee>()
                {
                    new Payee() { Category = "Y", Name = "3" },
                    new Payee() { Category = "X", Name = "2" },
                    new Payee() { Category = "Z", Name = "5" },
                    new Payee() { Category = "X", Name = "1" },
                    new Payee() { Category = "Y", Name = "4" }
                }
            );
        }

        [TestInitialize]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                //.UseLoggerFactory(logfact)
                .UseInMemoryDatabase(databaseName: "ApplicationDbContext")
                .Options;

            context = new ApplicationDbContext(options);
            repository = new PayeeRepository(context);
            controller = new AjaxPayeeController(repository);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Didn't actually solve anything. Keep it around for possible future problem
            //DetachAllEntities();

            // https://stackoverflow.com/questions/33490696/how-can-i-reset-an-ef7-inmemory-provider-between-unit-tests
            context?.Database.EnsureDeleted();
            context = default;
            controller = default;
            repository = default;
        }

        [TestMethod]
        public async Task Select()
        {
            await AddFive();
            var expected = repository.All.First();

            var actionresult = await controller.Select(expected.ID, true);

            Assert.That.IsOfType<OkResult>(actionresult);

            Assert.IsTrue(true == expected.Selected);
        }

        [TestMethod]
        public async Task Deselect()
        {
            await AddFive();
            var expected = repository.All.First();
            expected.Selected = true;
            await repository.UpdateAsync(expected);

            var actionresult = await controller.Select(expected.ID, false);

            Assert.That.IsOfType<OkResult>(actionresult);

            Assert.IsTrue(false == expected.Selected);
        }

        [TestMethod]
        public async Task Add()
        {
            var expected = new Payee() { Category = "B", Name = "3" };

            var actionresult = await controller.Add(expected);

            var objresult = Assert.That.IsOfType<ObjectResult>(actionresult);
            Assert.AreEqual(expected, objresult.Value);
            Assert.AreEqual(1, repository.All.Count());
        }

        [TestMethod]
        public async Task Edit()
        {
            await AddFive();
            var take1 = repository.All.Take(1);
            var original = take1.First();
            var id = original.ID;

            // Keep a deep copy for later comparison
            var copy = (await DeepCopy.MakeDuplicateOf(take1)).First();

            // detach the original. in real life we won't have another tracked object hanging around like this
            context.Entry(original).State = EntityState.Detached;

            var newitem = new Payee() { ID = id, Name = "I have edited you!", Category = original.Category };

            var actionresult = await controller.Edit(id, newitem);

            var objresult = Assert.That.IsOfType<ObjectResult>(actionresult);
            Assert.AreEqual(newitem, objresult.Value);
            Assert.AreNotEqual(original, objresult.Value);

            var actual = await context.Payees.Where(x => x.ID == id).SingleAsync();
            Assert.AreEqual(newitem, actual);
            Assert.AreNotEqual(copy, actual);
        }
    }
}
