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

        [DataRow("1-1")]
        [DataRow("12-1")]
        [DataRow("9-30")]
        [DataRow("12-10")]
        [TestMethod]
        public void MatchDate(string date)
        {
            // Given: A filename with a date in it
            var filename = $"{date}.pdf";

            // When: Constructing a receipt object from it
            var receipt = Receipt.FromFilename(filename);

            // Then: The timestamp is set with correct month & date values
            var expected = DateTime.Parse(date).Date;
            Assert.AreEqual(expected.Month, receipt.Timestamp.Month);
            Assert.AreEqual(expected.Date, receipt.Timestamp.Date);
        }
    }
}
