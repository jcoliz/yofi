using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using YoFi.Core.Models;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class ReceiptTest
    {
        [TestMethod]
        public void MatchNameOnly()
        {
            // Given: A filename with only a payee name in it
            var name = "Multiplex Cable Services";
            var filename = $"{name}.pdf";

            // When: Constructing a receipt object from it
            var receipt = Receipt.FromFilename(filename);

            // Then: The name is set
            Assert.AreEqual(name, receipt.Name);
        }
        [TestMethod]
        public void MatchAmountOnly()
        {
            // Given: A filename with only an amount in it
            var amount = 1234.45m;
            var filename = $"${amount:0.00}.pdf";

            // When: Constructing a receipt object from it
            var receipt = Receipt.FromFilename(filename);

            // Then: The name is set
            Assert.AreEqual(amount, receipt.Amount);
        }
    }
}
