using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxWeb.Asp.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ofx.Tests
{
    [TestClass]
    public class SplitTest
    {
        ApplicationDbContext context;

        [TestInitialize]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ApplicationDbContext")
                .Options;

            context = new ApplicationDbContext(options);
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(context);
        }
    }
}
