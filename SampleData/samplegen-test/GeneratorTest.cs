using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace YoFi.SampleGen.Tests
{
    [TestClass]
    public class GeneratorTest
    {
        #region Helpers
        private int NumPeriodsFor(SchemeEnum scheme) => Definition.SchemeNumPeriods[scheme];

        private IEnumerable<Transaction> SimpleTest(SchemeEnum scheme, JitterEnum datejitter = JitterEnum.None)
        {
            // Given: Scheme as supplied, No Jitter
            var periods = NumPeriodsFor(scheme);
            var periodicamount = 100m;
            var amount = periodicamount * periods;
            var item = new Definition() { Scheme = scheme, YearlyAmount = amount, AmountJitter = JitterEnum.None, DateJitter = datejitter, Category = "Category", Payee = "Payee" };

            // When: Generating transactions
            var actual = item.GetTransactions();

            // Then: There are the right amount of transactions
            Assert.AreEqual(periods, actual.Count());

            // And: For each transaction...
            foreach (var result in actual)
            {
                // And: The amounts are exactly as expected
                Assert.AreEqual(periodicamount, result.Amount);

                // And: The category and payee match
                Assert.AreEqual(item.Payee, result.Payee);
                Assert.AreEqual(item.Category, result.Category);
            }

            // And: Return to the test for more processing
            return actual;
        }
        #endregion

        #region No Jitter

        [TestMethod]
        public void MonthlySimple()
        {
            // Given: Monthly Scheme, No Jitter
            // When: Generating transactions
            // Then: Transactions pass all standard tests
            var actual = SimpleTest(SchemeEnum.Monthly);

            // And: They are all on the same day
            Assert.IsTrue(actual.All(x => x.Timestamp.Day == actual.First().Timestamp.Day));
        }


        [TestMethod]
        public void YearlySimple()
        {
            // Given: Yearly Scheme, No Jitter
            // When: Generating transactions
            // Then: Transactions pass all standard tests
            SimpleTest(SchemeEnum.Yearly);
        }

        [TestMethod]
        public void SemiMonthlySimple()
        {
            // Given: SemiMonthly Scheme, No Jitter
            // When: Generating transactions
            // Then: Transactions pass all standard tests
            var actual = SimpleTest(SchemeEnum.SemiMonthly);

            // And: They are all on the first or 15th
            Assert.IsTrue(actual.All(x => x.Timestamp.Day == 1 || x.Timestamp.Day == 15));
        }

        [TestMethod]
        public void QuarterlySimple()
        {
            // Given: Quarterly Scheme, No Jitter
            // When: Generating transactions for this specific year
            var year = 2000;
            Definition.Year = year;
            // Then: Transactions pass all standard tests
            var actual = SimpleTest(SchemeEnum.Quarterly);

            // And: They are all on the same day of the quarter
            var firstdayofquarter = Enumerable.Range(1, 4).Select(x => new DateTime(year, x * 3 - 2, 1));
            var daysofquarter = actual.Select(x => x.Timestamp.DayOfYear - firstdayofquarter.Last(y => x.Timestamp >= y).DayOfYear);
            Assert.IsTrue(daysofquarter.All(x => x == daysofquarter.First()));
        }

        [TestMethod]
        public void WeeklySimple()
        {
            // Given: Weekly Scheme, No Jitter
            // When: Generating transactions
            // Then: Transactions pass all standard tests
            var actual = SimpleTest(SchemeEnum.Weekly);

            // And: They are all on the same day of the week
            Assert.IsTrue(actual.All(x => x.Timestamp.DayOfWeek == actual.First().Timestamp.DayOfWeek));
        }

        [TestMethod]
        public void ManyPerWeekSimple()
        {
            // Given: Many Per Week Scheme, No Jitter
            // When: Generating transactions
            // Then: Transactions pass all standard tests
            var actual = SimpleTest(SchemeEnum.ManyPerWeek);

            // And: The days of week vary
            Assert.IsTrue(actual.Any(x => x.Timestamp.DayOfWeek != actual.First().Timestamp.DayOfWeek));
        }
        #endregion

        #region Amount Jitter

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

        [DataRow(SchemeEnum.Monthly, JitterEnum.Low)]
        [DataRow(SchemeEnum.Monthly, JitterEnum.Moderate)]
        [DataRow(SchemeEnum.Monthly, JitterEnum.High)]
        [DataRow(SchemeEnum.Quarterly, JitterEnum.Low)]
        [DataRow(SchemeEnum.Quarterly, JitterEnum.Moderate)]
        [DataRow(SchemeEnum.Quarterly, JitterEnum.High)]
        [DataRow(SchemeEnum.Weekly, JitterEnum.Low)]
        [DataRow(SchemeEnum.Weekly, JitterEnum.Moderate)]
        [DataRow(SchemeEnum.Weekly, JitterEnum.High)]
        [DataRow(SchemeEnum.ManyPerWeek, JitterEnum.Low)]
        [DataRow(SchemeEnum.ManyPerWeek, JitterEnum.Moderate)]
        [DataRow(SchemeEnum.ManyPerWeek, JitterEnum.High)]
        [DataTestMethod]
        public void AmountJitterMany(SchemeEnum scheme, JitterEnum jitter)
        {
            // Given: Monthly Scheme, Amount Jitter as supplied
            var periods = NumPeriodsFor(scheme);
            var amount = 100.00m;
            var item = new Definition() { Scheme = scheme, YearlyAmount = periods * amount, AmountJitter = jitter, DateJitter = JitterEnum.None, Category = "Category", Payee = "Payee" };

            // When: Generating transactions x100
            var numtries = 100;
            var actual = Enumerable.Repeat(1, numtries).SelectMany(x => item.GetTransactions());

            // And: The amounts vary
            Assert.IsTrue(actual.Any(x => x.Amount != actual.First().Amount));

            // And: The amounts are within the expected range for the supplied jitter
            var jittervalue = Definition.AmountJitterValues[jitter];
            var min = actual.Min(x => x.Amount);
            var max = actual.Max(x => x.Amount);
            Assert.AreEqual(((double)amount * (1 - jittervalue)), (double)min, (double)amount * (double)jittervalue / 5.0);
            Assert.AreEqual(((double)amount * (1 + jittervalue)), (double)max, (double)amount * (double)jittervalue / 5.0);
        }

        #endregion 

        #region Date Jitter

        [DataRow(JitterEnum.Low)]
        [DataRow(JitterEnum.Moderate)]
        [DataRow(JitterEnum.High)]
        [DataTestMethod]
        public void MonthlyDateJitterOnce(JitterEnum jitter)
        {
            // Given: Monthly Scheme, Date Jitter as supplied
            // When: Generating transactions
            // Then: Transactions pass all standard tests
            var scheme = SchemeEnum.Monthly;
            var actual = SimpleTest(scheme, datejitter:jitter);

            // And: The dates vary
            Assert.IsTrue(actual.Any(x => x.Timestamp.Day != actual.First().Timestamp.Day));

            // And: The date ranges are within the expected range for the supplied jitter
            var jittervalue = Definition.DateJitterValues[jitter];
            var min = actual.Min(x => x.Timestamp.Day);
            var max = actual.Max(x => x.Timestamp.Day);
            var actualrange = max - min;
            var expectedrange = Definition.SchemeTimespans[scheme].Days * (double)jittervalue;
            Assert.IsTrue(actualrange <= expectedrange);
        }

        [DataRow(JitterEnum.Low)]
        [DataRow(JitterEnum.Moderate)]
        [DataRow(JitterEnum.High)]
        [DataTestMethod]
        public void QuarterlyDateJitterOnce(JitterEnum jitter)
        {
            // Given: Quarterly Scheme, Date Jitter as supplied
            // When: Generating transactions for this specific year
            var year = 2000;
            Definition.Year = year;
            // Then: Transactions pass all standard tests
            var scheme = SchemeEnum.Quarterly;
            var actual = SimpleTest(scheme, datejitter: jitter);

            // And: The days within quarter vary
            var firstdayofquarter = Enumerable.Range(1, 4).Select(x => new DateTime(year, x * 3 - 2, 1));
            var daysofquarter = actual.Select(x => x.Timestamp.DayOfYear - firstdayofquarter.Last(y => x.Timestamp >= y).DayOfYear);
            Assert.IsTrue(daysofquarter.Any(x => x != daysofquarter.First()));

            // And: The date ranges are within the expected range for the supplied jitter
            var jittervalue = Definition.DateJitterValues[jitter];
            var min = daysofquarter.Min();
            var max = daysofquarter.Max();
            var actualrange = max - min;
            var expectedrange = Definition.SchemeTimespans[scheme].Days * (double)jittervalue;
            Assert.IsTrue(actualrange <= expectedrange);

            // Note: There are not enough quarters to be certain that the randomness will spread out
            // enough to test that the range is not too narrow.
        }

        //[DataRow(JitterEnum.Low)] // Note that Low Jitter on Weekly doesn't make much sense
        [DataRow(JitterEnum.Moderate)]
        [DataRow(JitterEnum.High)]
        [DataTestMethod]
        public void WeeklyDateJitterOnce(JitterEnum jitter)
        {
            // Given: Weekly Scheme, Date Jitter as supplied
            // When: Generating transactions for this specific year
            var year = 2000;
            Definition.Year = year;
            // Then: Transactions pass all standard tests
            var scheme = SchemeEnum.Weekly;
            var actual = SimpleTest(scheme, datejitter: jitter);

            // And: The days of week vary
            Assert.IsTrue(actual.Any(x => x.Timestamp.DayOfWeek != actual.First().Timestamp.DayOfWeek));

            // And: The day of week ranges are within the expected range for the supplied jitter

            // Note that I can't use DayOfWeek, because there are cases where the week wraps around
            var firstdayofweek = Enumerable.Range(0, 52).Select(x => new DateTime(year, 1, 1) + TimeSpan.FromDays(7*x));
            var daysofweek = actual.Select(x => x.Timestamp.DayOfYear - firstdayofweek.Last(y => x.Timestamp >= y).DayOfYear);
            var min = daysofweek.Min();
            var max = daysofweek.Max();
            var actualrange = max - min;

            var jittervalue = Definition.DateJitterValues[jitter];
            var expectedrange = Definition.SchemeTimespans[scheme].Days * (double)jittervalue;

            Assert.IsTrue(actualrange <= expectedrange);

            // Note: There are not enough quarters to be certain that the randomness will spread out
            // enough to test that the range is not too narrow.
        }
        #endregion

        #region Splits
        [TestMethod]
        public void SemiMonthlySplits()
        {
            // Given: SemiMonthly Scheme
            var scheme = SchemeEnum.SemiMonthly;
            var periods = NumPeriodsFor(scheme);
            var periodicamount = 100m;
            var amount = periodicamount * periods;
            var item = new Definition() { Scheme = scheme, DateJitter = JitterEnum.None, Payee = "Payee" };

            // And: A set of "split" category/amount items
            var splits = new List<Definition>()
            {
                new Definition() { Category = "1", YearlyAmount = periodicamount * periods * -1, AmountJitter = JitterEnum.None },
                new Definition() { Category = "2", YearlyAmount = periodicamount * periods * -2, AmountJitter = JitterEnum.None },
                new Definition() { Category = "3", YearlyAmount = periodicamount * periods * -3, AmountJitter = JitterEnum.None },
                new Definition() { Category = "4", YearlyAmount = periodicamount * periods * 10, AmountJitter = JitterEnum.None },
            };

            // When: Generating transactions
            var actual = item.GetTransactions(splits);

            // Then: There are the right amount of transactions
            Assert.AreEqual(periods, actual.Count());

            // And: They are all on the first or 15th
            Assert.IsTrue(actual.All(x => x.Timestamp.Day == 1 || x.Timestamp.Day == 15));

            // And: For each transaction...
            foreach (var result in actual)
            {
                // And: The amounts are exactly as expected
                Assert.AreEqual(periodicamount * 4, result.TotalAmount);

                // And: The payee matches
                Assert.AreEqual(item.Payee, result.Payee);

                // And: For each split...
                int i = splits.Count();
                while(i-- > 0)
                {
                    // And: The category matches
                    Assert.AreEqual(splits[i].Category, result.Splits[i].Category);

                    // And: The amounts are exactly as expected
                    Assert.AreEqual(splits[i].YearlyAmount / periods, result.Splits[i].Amount);
                }
            }
        }

        #endregion
    }
}
