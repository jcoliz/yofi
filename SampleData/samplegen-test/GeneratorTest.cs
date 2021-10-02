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
            Assert.AreEqual((double)(amount * (1 - jittervalue)),(double)min, (double)amount * (double)jittervalue / 5.0);
            Assert.AreEqual((double)(amount * (1 + jittervalue)), (double)max, (double)amount * (double)jittervalue / 5.0);
        }
    }
}
