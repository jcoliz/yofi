using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using YoFi.Core.Importers;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class UniversalImporterTest
    {
        UniversalImporter importer;

        [TestInitialize]
        public void SetUp()
        {
            importer = new UniversalImporter(null, null, null);
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(importer);
        }
    }
}
