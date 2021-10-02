using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace YoFi.SampleGen.Tests
{
    [TestClass]
    public class GeneratorTest
    {
        [TestMethod]
        public void YearlySimple()
        {
            // Given: Yearly Scheme, No Jitter
            var item = new Definition() { Scheme = SchemeEnum.Yearly, YearlyAmount = 1234.56m, AmountJitter = JitterEnum.None, DateJitter = JitterEnum.None, Category = "Category", Payee = "Payee" };

            // When: Generating transactions
            var actual = item.GetTransactions();

            // Then: There is only one transaction (it's yearly)
            Assert.AreEqual(1, actual.Count());

            // And: The amount is exactly what's in the definition
            Assert.AreEqual(item.YearlyAmount, actual.Single().Amount);

            // And: The category and payee match
            Assert.AreEqual(item.Payee, actual.Single().Payee);
            Assert.AreEqual(item.Category, actual.Single().Category);
        }

        [DataRow(JitterEnum.Low)]
        [DataRow(JitterEnum.Moderate)]
        [DataRow(JitterEnum.High)]
        [DataTestMethod]
        public void YearlyAmountJitter(JitterEnum jitter)
        {
            // Given: Yearly Scheme, Amount Jitter as supplied
            var amount = 1234.56m;
            var item = new Definition() { Scheme = SchemeEnum.Yearly, YearlyAmount = amount, AmountJitter = jitter, DateJitter = JitterEnum.None, Category = "Category", Payee = "Payee" };

            // When: Generating transactions x100
            var numtries = 100;
            var actual = Enumerable.Repeat(1, numtries).SelectMany(x => item.GetTransactions());

            // Then: There is only one transaction per time we called
            Assert.AreEqual(numtries, actual.Count());

            // And: The amounts vary
            Assert.IsTrue(actual.Any(x => x.Amount != actual.First().Amount));

            // And: The amounts are within the expected range for the supplied jitter
            var jittervalue = Definition.AmountJitterValues[jitter];
            var min = actual.Min(x => x.Amount);
            var max = actual.Max(x => x.Amount);
            Assert.AreEqual(((double)amount * (1 - jittervalue)), (double)min, (double)amount * jittervalue / 5.0);
            Assert.AreEqual(((double)amount * (1 + jittervalue)), (double)max, (double)amount * jittervalue / 5.0);
        }

        [TestMethod]
        public void MonthlySimple()
        {
            // Given: Monthly Scheme, No Jitter
            var amount = 1200.00m;
            var item = new Definition() { Scheme = SchemeEnum.Monthly, YearlyAmount = amount, AmountJitter = JitterEnum.None, DateJitter = JitterEnum.None, Category = "Category", Payee = "Payee" };

            // When: Generating transactions
            var actual = item.GetTransactions();

            // Then: There are exactly 12 transactions (it's monthly)
            Assert.AreEqual(12, actual.Count());

            // And: They are all on the same day
            Assert.IsTrue(actual.All(x => x.Timestamp.Day == actual.First().Timestamp.Day));

            // And: For each transaction...
            foreach (var result in actual)
            {
                // And: The amounts are exactly 1/12 what's in the definition
                Assert.AreEqual(amount / 12, result.Amount);

                // And: The category and payee match
                Assert.AreEqual(item.Payee, result.Payee);
                Assert.AreEqual(item.Category, result.Category);
            }
        }

        [DataRow(JitterEnum.Low)]
        [DataRow(JitterEnum.Moderate)]
        [DataRow(JitterEnum.High)]
        [DataTestMethod]
        public void MonthlyAmountJitterOnce(JitterEnum jitter)
        {
            // Given: Monthly Scheme, Amount Jitter as supplied
            var amount = 100.00m;
            var item = new Definition() { Scheme = SchemeEnum.Monthly, YearlyAmount = 12 * amount, AmountJitter = jitter, DateJitter = JitterEnum.None, Category = "Category", Payee = "Payee" };

            // When: Generating transactions
            var actual = item.GetTransactions();

            // Then: There are exactly 12 transactions (it's monthly)
            Assert.AreEqual(12, actual.Count());

            // And: The amounts vary
            Assert.IsTrue(actual.Any(x => x.Amount != actual.First().Amount));

            // And: The amounts are within the expected range for the supplied jitter
            var jittervalue = Definition.AmountJitterValues[jitter];
            var min = actual.Min(x => x.Amount);
            var max = actual.Max(x => x.Amount);
            Assert.IsTrue(min >= amount * (1 - (decimal)jittervalue));
            Assert.IsTrue(max <= amount * (1 + (decimal)jittervalue));
        }

        [DataRow(JitterEnum.Low)]
        [DataRow(JitterEnum.Moderate)]
        [DataRow(JitterEnum.High)]
        [DataTestMethod]
        public void MonthlyAmountJitterMany(JitterEnum jitter)
        {
            // Given: Monthly Scheme, Amount Jitter as supplied
            var amount = 100.00m;
            var item = new Definition() { Scheme = SchemeEnum.Monthly, YearlyAmount = 12 * amount, AmountJitter = jitter, DateJitter = JitterEnum.None, Category = "Category", Payee = "Payee" };

            // When: Generating transactions x100
            var numtries = 100;
            var actual = Enumerable.Repeat(1, numtries).SelectMany(x => item.GetTransactions());

            // Then: There are 12 transactions per time we called
            Assert.AreEqual(numtries * 12, actual.Count());

            // And: The amounts vary
            Assert.IsTrue(actual.Any(x => x.Amount != actual.First().Amount));

            // And: The amounts are within the expected range for the supplied jitter
            var jittervalue = Definition.AmountJitterValues[jitter];
            var min = actual.Min(x => x.Amount);
            var max = actual.Max(x => x.Amount);
            Assert.AreEqual(((double)amount * (1 - jittervalue)), (double)min, (double)amount * (double)jittervalue / 5.0);
            Assert.AreEqual(((double)amount * (1 + jittervalue)), (double)max, (double)amount * (double)jittervalue / 5.0);
        }


        [DataRow(JitterEnum.Low)]
        [DataRow(JitterEnum.Moderate)]
        [DataRow(JitterEnum.High)]
        [DataTestMethod]
        public void MonthlyDateJitterOnce(JitterEnum jitter)
        {
            // Given: Monthly Scheme, Date Jitter as supplied
            var amount = 100.00m;
            var item = new Definition() { Scheme = SchemeEnum.Monthly, YearlyAmount = 12 * amount, AmountJitter = JitterEnum.None, DateJitter = jitter, Category = "Category", Payee = "Payee" };

            // When: Generating transactions
            var actual = item.GetTransactions();

            // Then: There are exactly 12 transactions (it's monthly)
            Assert.AreEqual(12, actual.Count());

            // And: The amounts are the same
            Assert.IsTrue(actual.All(x => x.Amount == actual.First().Amount));

            // And: The dates vary
            Assert.IsTrue(actual.Any(x => x.Timestamp.Day != actual.First().Timestamp.Day));

            // And: The amounts are within the expected range for the supplied jitter
            var jittervalue = Definition.DateJitterValues[jitter];
            var min = actual.Min(x => x.Timestamp.Day);
            var max = actual.Max(x => x.Timestamp.Day);
            var actualrange = max - min;
            var expectedrange = Definition.SchemeTimespans[item.Scheme].Days * (double)jittervalue;
            Assert.IsTrue(actualrange <= expectedrange);
            Assert.IsTrue(actualrange > expectedrange / 2);
        }

        [TestMethod]
        public void QuarterlySimple()
        {
            // Given: Monthly Scheme, No Jitter
            var amount = 1200.00m;
            var item = new Definition() { Scheme = SchemeEnum.Quarterly, YearlyAmount = amount, AmountJitter = JitterEnum.None, DateJitter = JitterEnum.None, Category = "Category", Payee = "Payee" };

            // When: Generating transactions for this specific year
            var year = 2000;
            Definition.Year = year;
            var actual = item.GetTransactions();

            // Then: There are exactly 4 transactions
            Assert.AreEqual(4, actual.Count());

            // And: They are all on the same day of the quarter
            var firstdayofquarter = Enumerable.Range(1, 4).Select(x => new DateTime(year, x * 3 - 2, 1));
            var daysofquarter = actual.Select(x => x.Timestamp.DayOfYear - firstdayofquarter.Last(y => x.Timestamp >= y).DayOfYear);
            Assert.IsTrue(daysofquarter.All(x => x == daysofquarter.First()));
            
            // And: For each transaction...
            foreach (var result in actual)
            {
                // And: The amounts are exactly 1/4 what's in the definition
                Assert.AreEqual(amount / 4, result.Amount);

                // And: The category and payee match
                Assert.AreEqual(item.Payee, result.Payee);
                Assert.AreEqual(item.Category, result.Category);
            }
        }
    }
}
