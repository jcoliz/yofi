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

        [DataRow(-1,"Invalid")]
        [DataRow(0, "Yearly")]
        [DataRow(26, "26")]
        [DataTestMethod]
        public void FrequencyToString(int frequency, string expected)
        {
            // Given: A budgettx with the given {frequency}
            var tx = new BudgetTx() { Frequency = frequency };

            // When: Asking for the word representation of it
            var actual = tx.FrequencyName;

            // Then: That matches the {expected} value
            Assert.AreEqual(expected, actual);
        }

        [DataTestMethod]
        public void FrequencyToEnum()
        {
            // Stepping through the frequency enum
            foreach(var o in Enum.GetValues(typeof(BudgetTx.FrequencyEnum)))
            {
                // Ensure that the string is returned as expected for each value
                var e = (BudgetTx.FrequencyEnum)o;
                FrequencyToString((int)e, e.ToString());
            }
        }
    }
}
