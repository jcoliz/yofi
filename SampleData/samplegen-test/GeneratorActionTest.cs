using Common.NET.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YoFi.SampleGen.Tests
{
    [TestClass]
    public class GeneratorActionTest
    {
        [TestMethod]
        public void Loader()
        {
            // Given: An existing file of defitions
            var stream = TestData.Open("TestData1.xlsx");

            // When: Loading them
            var generator = new SampleDataGenerator();
            generator.LoadDefinitions(stream);

            // Then: They are all loaded
            Assert.AreEqual(32, generator.Definitions.Count);

            // And: Quick spot check of schemes looks good
            Assert.AreEqual(6, generator.Definitions.Count(x => x.Scheme == SchemeEnum.Quarterly));
            Assert.AreEqual(2, generator.Definitions.Count(x => x.AmountJitter == JitterEnum.Moderate));
        }
    }
}
