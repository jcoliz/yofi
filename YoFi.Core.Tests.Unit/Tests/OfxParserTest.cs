using Common.DotNet.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxSharp;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core.Tests.Unit
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
                var filename = "SampleData-2022-Full-Month02.ofx";
                var stream = Common.DotNet.Test.SampleData.Open(filename);

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
            Assert.AreEqual(73, Document.Statements.SelectMany(x=>x.Transactions).Count());
        }

        [TestMethod]
        public void TransactionSample()
        {
            var actual = Document.Statements.First().Transactions.First();

            Assert.AreEqual(-138.42m, actual.Amount);
            Assert.AreEqual("Spaghetti Factory", actual.Memo.Trim());
            Assert.AreEqual(new DateTime(2022, 2, 1), actual.Date.Value.DateTime);
        }
    }
}
