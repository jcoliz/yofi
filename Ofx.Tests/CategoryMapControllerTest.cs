﻿using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxWeb.Asp.Controllers;
using OfxWeb.Asp.Data;

//
// This is my first controller unit test. I am going to start with an easy controller, the category map controller.
//

namespace Ofx.Tests
{
    [TestClass]
    public class CategoryMapControllerTest
    {
        [TestMethod]
        public void Null()
        {
            var tested = new CategoryMapsController(null);

            Assert.IsNotNull(tested);
        }

        [TestMethod]
        public void Empty()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ApplicationDbContext")
                .Options;

            var context = new ApplicationDbContext(options);

            var tested = new CategoryMapsController(context);

            Assert.IsNotNull(tested);
        }
    }
}
