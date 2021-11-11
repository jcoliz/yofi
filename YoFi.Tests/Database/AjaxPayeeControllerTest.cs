using Common.DotNet.Test;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.AspNet.Controllers;
using YoFi.AspNet.Data;
using YoFi.Core;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.Tests.Database
{
    [TestClass]
    public class AjaxPayeeControllerTest
    {
        private AjaxPayeeController controller;
        private IPayeeRepository repository;
        private IDataContext context;

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

        async Task AddFivePayees()
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

        [TestMethod]
        public async Task SelectPayeeId()
        {
            await AddFivePayees();
            var expected = repository.All.First();

            var actionresult = await controller.Select(expected.ID, true);

            Assert.That.IsOfType<OkResult>(actionresult);

            Assert.IsTrue(true == expected.Selected);
        }

    }
}
