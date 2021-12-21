using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YoFi.Core.Models;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class BudgetTxTest
    {
        [DataRow(12)]
        [DataTestMethod]
        public void ReportableDivides(int frequency)
        {
            var amount = 5200 * 3m;
            var tx = new BudgetTx() { Amount = amount, Frequency = frequency, Timestamp = new DateTime(2021, 1, 1), Category = "A:B" };

            var reportables = tx.Reportables;

            Assert.AreEqual(frequency, reportables.Count());
            Assert.IsTrue(reportables.All(x => x.Amount == amount / frequency));
            Assert.IsTrue(reportables.All(x => x.Category == tx.Category));
        }
    }
}
