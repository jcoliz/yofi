using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Common.AspNet;

namespace YoFi.Tests.Attributers
{
    [TestClass]
    public class ValidateFilesProvidedAttributeTest
    {
        ValidateFilesProvidedAttribute attribute;

        [TestInitialize]
        public void SetUp()
        {   
            attribute = new ValidateFilesProvidedAttribute();
        }

        [TestMethod]
        public void Empty()
        {
            // When: Creating a new object
            // (in setup)

            // Then: It's valid
            Assert.IsNotNull(attribute);
        }

    }
}
