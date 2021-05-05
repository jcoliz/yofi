using Microsoft.EntityFrameworkCore;
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
        CategoryMapsController controller = null;

        [TestInitialize]
        public void SetUp()
        {
            if (null == controller)
            {
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(databaseName: "ApplicationDbContext")
                    .Options;

                var context = new ApplicationDbContext(options);

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
    }
}
