using Common.AspNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace YoFi.Tests.Attributes
{
    [TestClass]
    public class ValidateStorageAttributeTest
    {
        private ValidateStorageAvailableAttribute attribute;

        [TestInitialize]
        public void SetUp()
        {
            //next_called = 0;
            attribute = new ValidateStorageAvailableAttribute();
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
