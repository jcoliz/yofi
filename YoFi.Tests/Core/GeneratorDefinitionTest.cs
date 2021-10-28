using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using YoFi.Core.Models;
using YoFi.Core.SampleGen;

namespace YoFi.Tests.Core.SampleGen
{
    [TestClass]
    public class GeneratorDefinitionTest
    {
        #region Helpers
        private int NumPeriodsFor(FrequencyEnum scheme) => SampleDataPattern.FrequencyPerYear[scheme];

        private IEnumerable<Transaction> SimpleTest(FrequencyEnum scheme, JitterEnum datejitter = JitterEnum.None, int numperperiod = 1)
        {
            // Given: Scheme as supplied, No Jitter
            var periods = NumPeriodsFor(scheme);
            var periodicamount = 100m;
            var amount = periodicamount * periods * numperperiod;
            var item = new SampleDataPattern() { DateFrequency = scheme, AmountYearly = amount, AmountJitter = JitterEnum.None, DateJitter = datejitter, DateRepeats = numperperiod, Category = "Category", Payee = "Payee" };

            // When: Generating transactions
            var actual = item.GetTransactions();

            // Then: There are the right amount of transactions
            Assert.AreEqual(periods * numperperiod, actual.Count());

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

        #region Invididual features

        [TestMethod]
        public void MultiplePayees()
        {
            // Given: Weekly definition with multiple payee options
            var payees = new List<string>() { "First", "Second", "Third" };
            var item = new SampleDataPattern() { DateFrequency = FrequencyEnum.Weekly, AmountYearly = 5200m, AmountJitter = JitterEnum.None, DateJitter = JitterEnum.None, Category = "Category", Payee = string.Join(",",payees) };

            // When: Generating transactions
            var actual = item.GetTransactions();

            // Then: The Payees are different
            Assert.IsTrue(actual.Any(x => x.Payee != actual.First().Payee));

            // And: All the payees are one of the expected payees
            Assert.IsTrue(actual.All(x => payees.Contains(x.Payee)));
        }

        #endregion


        #region No Jitter

        [TestMethod]
        public void MonthlySimple()
        {
            // Given: Monthly Scheme, No Jitter
            // When: Generating transactions
            // Then: Transactions pass all standard tests
            var actual = SimpleTest(FrequencyEnum.Monthly);

            // And: They are all on the same day
            Assert.IsTrue(actual.All(x => x.Timestamp.Day == actual.First().Timestamp.Day));
        }


        [TestMethod]
        public void YearlySimple()
        {
            // Given: Yearly Scheme, No Jitter
            // When: Generating transactions
            // Then: Transactions pass all standard tests
            SimpleTest(FrequencyEnum.Yearly);
        }

        [TestMethod]
        public void SemiMonthlySimple()
        {
            // Given: SemiMonthly Scheme, No Jitter
            // When: Generating transactions
            // Then: Transactions pass all standard tests
            var actual = SimpleTest(FrequencyEnum.SemiMonthly);

            // And: They are all on the first or 15th
            Assert.IsTrue(actual.All(x => x.Timestamp.Day == 1 || x.Timestamp.Day == 15));
        }

        [TestMethod]
        public void QuarterlySimple()
        {
            // Given: Quarterly Scheme, No Jitter
            // When: Generating transactions for this specific year
            var year = 2000;
            SampleDataPattern.Year = year;
            // Then: Transactions pass all standard tests
            var actual = SimpleTest(FrequencyEnum.Quarterly);

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
            var actual = SimpleTest(FrequencyEnum.Weekly);

            // And: They are all on the same day of the week
            Assert.IsTrue(actual.All(x => x.Timestamp.DayOfWeek == actual.First().Timestamp.DayOfWeek));
        }

        [TestMethod]
        public void ManyPerWeekSimple()
        {
            // Given: Weekly Scheme, DateRepeats = 3, No Jitter
            // When: Generating transactions
            // Then: Transactions pass all standard tests
            var actual = SimpleTest(FrequencyEnum.Weekly, datejitter:JitterEnum.High, numperperiod: 3);

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
            var item = new SampleDataPattern() { DateFrequency = FrequencyEnum.Yearly, AmountYearly = amount, AmountJitter = jitter, DateJitter = JitterEnum.None, Category = "Category", Payee = "Payee" };

            // When: Generating transactions x100
            var numtries = 100;
            var actual = Enumerable.Repeat(1, numtries).SelectMany(x => item.GetTransactions());

            // Then: There is only one transaction per time we called
            Assert.AreEqual(numtries, actual.Count());

            // And: The amounts vary
            Assert.IsTrue(actual.Any(x => x.Amount != actual.First().Amount));

            // And: The amounts are within the expected range for the supplied jitter
            var jittervalue = SampleDataPattern.AmountJitterValues[jitter];
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
            var item = new SampleDataPattern() { DateFrequency = FrequencyEnum.Monthly, AmountYearly = 12 * amount, AmountJitter = jitter, DateJitter = JitterEnum.None, Category = "Category", Payee = "Payee" };

            // When: Generating transactions
            var actual = item.GetTransactions();

            // Then: There are exactly 12 transactions (it's monthly)
            Assert.AreEqual(12, actual.Count());

            // And: The amounts vary
            Assert.IsTrue(actual.Any(x => x.Amount != actual.First().Amount));

            // And: The amounts are within the expected range for the supplied jitter
            var jittervalue = SampleDataPattern.AmountJitterValues[jitter];
            var min = actual.Min(x => x.Amount);
            var max = actual.Max(x => x.Amount);
            Assert.IsTrue(min >= amount * (1 - (decimal)jittervalue));
            Assert.IsTrue(max <= amount * (1 + (decimal)jittervalue));
        }

        [DataRow(FrequencyEnum.Monthly, JitterEnum.Low)]
        [DataRow(FrequencyEnum.Monthly, JitterEnum.Moderate)]
        [DataRow(FrequencyEnum.Monthly, JitterEnum.High)]
        [DataRow(FrequencyEnum.Quarterly, JitterEnum.Low)]
        [DataRow(FrequencyEnum.Quarterly, JitterEnum.Moderate)]
        [DataRow(FrequencyEnum.Quarterly, JitterEnum.High)]
        [DataRow(FrequencyEnum.Weekly, JitterEnum.Low)]
        [DataRow(FrequencyEnum.Weekly, JitterEnum.Moderate)]
        [DataRow(FrequencyEnum.Weekly, JitterEnum.High)]
        [DataTestMethod]
        public void AmountJitterMany(FrequencyEnum scheme, JitterEnum jitter)
        {
            // Given: Monthly Scheme, Amount Jitter as supplied
            var periods = NumPeriodsFor(scheme);
            var amount = 100.00m;
            var item = new SampleDataPattern() { DateFrequency = scheme, AmountYearly = periods * amount, AmountJitter = jitter, DateJitter = JitterEnum.None, Category = "Category", Payee = "Payee" };

            // When: Generating transactions x100
            var numtries = 100;
            var actual = Enumerable.Repeat(1, numtries).SelectMany(x => item.GetTransactions());

            // And: The amounts vary
            Assert.IsTrue(actual.Any(x => x.Amount != actual.First().Amount));

            // And: The amounts are within the expected range for the supplied jitter
            var jittervalue = SampleDataPattern.AmountJitterValues[jitter];
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
            var scheme = FrequencyEnum.Monthly;
            var actual = SimpleTest(scheme, datejitter:jitter);

            // And: The dates vary
            Assert.IsTrue(actual.Any(x => x.Timestamp.Day != actual.First().Timestamp.Day));

            // And: The date ranges are within the expected range for the supplied jitter
            var jittervalue = SampleDataPattern.DateJitterValues[jitter];
            var min = actual.Min(x => x.Timestamp.Day);
            var max = actual.Max(x => x.Timestamp.Day);
            var actualrange = max - min;
            var expectedrange = SampleDataPattern.SchemeTimespans[scheme].Days * (double)jittervalue;
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
            SampleDataPattern.Year = year;
            // Then: Transactions pass all standard tests
            var scheme = FrequencyEnum.Quarterly;
            var actual = SimpleTest(scheme, datejitter: jitter);

            // And: The days within quarter vary
            var firstdayofquarter = Enumerable.Range(1, 4).Select(x => new DateTime(year, x * 3 - 2, 1));
            var daysofquarter = actual.Select(x => x.Timestamp.DayOfYear - firstdayofquarter.Last(y => x.Timestamp >= y).DayOfYear);
            Assert.IsTrue(daysofquarter.Any(x => x != daysofquarter.First()));

            // And: The date ranges are within the expected range for the supplied jitter
            var jittervalue = SampleDataPattern.DateJitterValues[jitter];
            var min = daysofquarter.Min();
            var max = daysofquarter.Max();
            var actualrange = max - min;
            var expectedrange = SampleDataPattern.SchemeTimespans[scheme].Days * (double)jittervalue;
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
            SampleDataPattern.Year = year;
            // Then: Transactions pass all standard tests
            var scheme = FrequencyEnum.Weekly;
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

            var jittervalue = SampleDataPattern.DateJitterValues[jitter];
            var expectedrange = SampleDataPattern.SchemeTimespans[scheme].Days * (double)jittervalue;

            Assert.IsTrue(actualrange <= expectedrange);

            // Note: There are not enough quarters to be certain that the randomness will spread out
            // enough to test that the range is not too narrow.
        }
        #endregion

        #region Splits

        [DataRow(FrequencyEnum.SemiMonthly)]
        [DataRow(FrequencyEnum.Monthly)]
        [DataRow(FrequencyEnum.Quarterly)]
        [DataRow(FrequencyEnum.Weekly)]
        [DataRow(FrequencyEnum.Yearly)]
        [DataTestMethod]
        public void Splits(FrequencyEnum scheme)
        {
            // Given: SemiMonthly Scheme
            var periods = NumPeriodsFor(scheme);
            var periodicamount = 100m;
            var item = new SampleDataPattern() { DateFrequency = scheme, DateJitter = JitterEnum.None, Payee = "Payee" };

            // And: A set of "split" category/amount items
            var splits = new List<SampleDataPattern>()
            {
                new SampleDataPattern() { Category = "1", AmountYearly = periodicamount * periods * -1, AmountJitter = JitterEnum.None },
                new SampleDataPattern() { Category = "2", AmountYearly = periodicamount * periods * -2, AmountJitter = JitterEnum.None },
                new SampleDataPattern() { Category = "3", AmountYearly = periodicamount * periods * -3, AmountJitter = JitterEnum.None },
                new SampleDataPattern() { Category = "4", AmountYearly = periodicamount * periods * 10, AmountJitter = JitterEnum.None },
            };

            // When: Generating transactions
            var actual = item.GetTransactions(splits);

            // Then: There are the right amount of transactions
            Assert.AreEqual(periods, actual.Count());

            // And: For each transaction...
            foreach (var result in actual)
            {
                // And: The amounts are exactly as expected
                Assert.AreEqual(periodicamount * (10 - 3 - 2 - 1), result.Amount);

                // And: The payee matches
                Assert.AreEqual(item.Payee, result.Payee);

                // And: For each split...
                int i = splits.Count();
                while(i-- > 0)
                {
                    // And: The category matches
                    Assert.AreEqual(splits[i].Category, result.Splits.Skip(i).First().Category);

                    // And: The amounts are exactly as expected
                    Assert.AreEqual(splits[i].AmountYearly / periods, result.Splits.Skip(i).First().Amount);
                }
            }
        }
        #endregion

        #region Others

        [TestMethod]
        public void Rounding()
        {
            // Given: One monthly pattern with no jitter, which will leave fractional pennies when divided
            var amount = 100m;
            var item = new SampleDataPattern() { DateFrequency = FrequencyEnum.Monthly, AmountYearly = amount, AmountJitter = JitterEnum.None, DateJitter = JitterEnum.None, Category = "Category", Payee = "Payee" };

            // When: Generating transactions
            var actual = item.GetTransactions();

            // Then: The resulting transactions have only two decimal places
            Assert.AreEqual(8.33m, actual.First().Amount);
        }

        #endregion
    }
}
