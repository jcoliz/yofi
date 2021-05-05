using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxWeb.Asp.Controllers;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System.Collections.Generic;
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
            if (null == controller)
            {
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(databaseName: "ApplicationDbContext")
                    .Options;

                context = new ApplicationDbContext(options);

                controller = new CategoryMapsController(context);
            }
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
    }
}
