using Common.NET.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxSharp;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class OfxParserTest
    {
        public OfxDocument Document = null;

        [TestInitialize]
        public async Task SetUp()
        {
            if (null == Document)
            {
                var filename = "FullSampleData-Month02.ofx";
                var stream = SampleData.Open(filename);

                Document = await OfxDocumentReader.FromSgmlFileAsync(stream);
            }
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(Document);
        }

        [TestMethod]
        public void TransactionsCount()
        {
            Assert.AreEqual(74, Document.Statements.SelectMany(x=>x.Transactions).Count());
        }

        [TestMethod]
        public void TransactionSample()
        {
            var actual = Document.Statements.First().Transactions.First();

            Assert.AreEqual(2569.92m, actual.Amount);
            Assert.AreEqual("Megacorp Inc", actual.Memo.Trim());
            Assert.AreEqual(new DateTime(2021, 2, 1), actual.Date.Value.DateTime);
        }
    }
}
