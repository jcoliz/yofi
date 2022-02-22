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

        [DataRow("0.01")]
        [DataRow("0")]
        [DataRow("1234")]
        [DataRow("1234.56")]
        [DataTestMethod]
        public void MatchAmountOnly(string input)
        {
            // Given: A filename with only an amount in it
            var amount = decimal.Parse(input);
            var filename = $"${amount}.pdf";

            // When: Constructing a receipt object from it
            var receipt = Receipt.FromFilename(filename);

            // Then: The amount is set
            Assert.AreEqual(amount, receipt.Amount);
        }

        [TestMethod]
        public void MatchNameAndAmount()
        {
            // Given: A filename with a name and amount in it
            var name = "Multiplex Cable Services";
            var amount = 1234.56m;
            var filename = $"{name} ${amount}.pdf";

            // When: Constructing a receipt object from it
            var receipt = Receipt.FromFilename(filename);

            // Then: The name is set
            Assert.AreEqual(name, receipt.Name);

            // Then: The amount is set
            Assert.AreEqual(amount, receipt.Amount);
        }
    }
}
