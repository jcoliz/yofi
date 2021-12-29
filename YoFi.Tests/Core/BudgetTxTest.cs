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

        [DataRow(-1, "Invalid")]
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

        [DataRow("Garbage", 1)]
        [DataTestMethod]
        public void StringToFrequency(string frequency, int expected)
        {
            // Given: An empty budgettx, with an invalid frequency
            var tx = new BudgetTx() { Frequency = -1 };

            // When: Setting the frequency using the "FrequencyName" field
            tx.FrequencyName = frequency;

            // Then: The resulting int value matches the {expected} value
            var actual = tx.Frequency;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void FrequencyToEnum()
        {
            // Stepping through the frequency enum
            foreach (var o in Enum.GetValues(typeof(BudgetTx.FrequencyEnum)))
            {
                // Ensure that the string is returned as expected for each value
                var e = (BudgetTx.FrequencyEnum)o;
                FrequencyToString((int)e, e.ToString());
            }
        }

        [TestMethod]
        public void EnumToFrequency()
        {
            // Stepping through the frequency enum
            foreach (var o in Enum.GetValues(typeof(BudgetTx.FrequencyEnum)))
            {
                // Ensure that the string is returned as expected for each value
                var e = (BudgetTx.FrequencyEnum)o;
                StringToFrequency(e.ToString(), (int)e);
            }
        }

        // NOTE: There is not a great way to test ImportEquals indirectly, because its only
        // called when there is a hash collision. SO we have to test it directly.
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void IComparerExplictNull()
        {
            // Given: An empty tx
            var tx = new BudgetTx();
            var i = (IImportDuplicateComparable)tx;

            // When: Comparing it to null            
            i.ImportEquals(null);

            // Then: Throws exceltion
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IComparerWrongType()
        {
            // Given: An empty tx
            var tx = new BudgetTx();
            var i = (IImportDuplicateComparable)tx;

            // When: Comparing it to some other type
            i.ImportEquals(new Transaction());

            // Then: Throws exceltion
        }

        [DynamicData(nameof(IImportDuplicateComparable_Index), DynamicDataSourceType.Method)]
        [DataTestMethod]
        public void IImportDuplicateComparable_Values(int firsti, int secondi, bool expected)
        {
            // Given: A pair of budget tx
            var first = IImportDuplicateComparable_Items[firsti];
            var second = IImportDuplicateComparable_Items[secondi];

            // When: Comparing them
            // Then: They are equal or not, as expected
            var i = (IImportDuplicateComparable)first;
            var actual = i.ImportEquals(second);

            Assert.AreEqual(expected, actual);
        }

        public static IEnumerable<object[]> IImportDuplicateComparable_Index()
            => new List<object[]>()
            {
                new object[] { 0, 0, true },
                new object[] { 1, 2, true },
                new object[] { 1, 3, true },
                new object[] { 1, 4, false },
                new object[] { 1, 5, false },
                new object[] { 1, 6, false }
            };

        public static List<BudgetTx> IImportDuplicateComparable_Items { get; } =
            new List<BudgetTx>()
            {
                new BudgetTx(),
                new BudgetTx() { Timestamp = new DateTime(2020,1,1), Category = "A", Memo = "M1" },
                new BudgetTx() { Timestamp = new DateTime(2020,1,1), Category = "A", Memo = "M2" },
                new BudgetTx() { Timestamp = new DateTime(2020,1,2), Category = "A", Memo = "M1" },
                new BudgetTx() { Timestamp = new DateTime(2020,1,1), Category = "B", Memo = "M1" },
                new BudgetTx() { Timestamp = new DateTime(2020,2,1), Category = "A", Memo = "M1" },
                new BudgetTx() { Timestamp = new DateTime(2019,1,1), Category = "A", Memo = "M1" },
            };
    }
}
