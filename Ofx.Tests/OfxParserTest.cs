using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxSharpLib;
using System.Linq;
using System.Reflection;

namespace Ofx.Tests
{
    [TestClass]
    public class OfxParserTest
    {
        public OfxDocument Document = null;

        [TestInitialize]
        public void SetUp()
        {
            if (null == Document)
            {
                var filename = "ExportedTransactions.ofx";
                var stream = SampleData.Open(filename);
                var parser = new OfxDocumentParser();
                Document = parser.Import(stream);
            }
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(Document);
        }
    }
}
