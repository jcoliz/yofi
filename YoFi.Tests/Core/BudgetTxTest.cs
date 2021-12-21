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
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(6)]
        [DataRow(12)]
        [DataTestMethod]
        public void ReportableEvenMonths(int frequency)
        {
            var amount = 5200 * 3m;
            var tx = new BudgetTx() { Amount = amount, Frequency = frequency, Timestamp = new DateTime(2021, 1, 1), Category = "A:B" };

            var reportables = tx.Reportables;

            Assert.AreEqual(frequency, reportables.Count());
            Assert.IsTrue(reportables.All(x => x.Amount == amount / frequency));
            Assert.IsTrue(reportables.All(x => x.Category == tx.Category));

            // This evenly divides on months, so should always be on the 1st of the month.
            Assert.IsTrue(reportables.All(x => x.Timestamp.Day == 1));
            Assert.IsTrue(reportables.All(x => x.Timestamp.Year == tx.Timestamp.Year));
        }

        [DataRow(52)]
        [DataRow(26)]
        [DataRow(13)]
        [DataTestMethod]
        public void ReportableEvenWeeks(int frequency)
        {
            var amount = 5200 * 3m;
            var tx = new BudgetTx() { Amount = amount, Frequency = frequency, Timestamp = new DateTime(2021, 1, 1), Category = "A:B" };

            var reportables = tx.Reportables;

            Assert.AreEqual(frequency, reportables.Count());
            Assert.IsTrue(reportables.All(x => x.Amount == amount / frequency));
            Assert.IsTrue(reportables.All(x => x.Category == tx.Category));

            // This evenly divides on months, so should always be on the same day of week
            Assert.IsTrue(reportables.All(x => x.Timestamp.DayOfWeek == reportables.First().Timestamp.DayOfWeek));
        }
    }
}
