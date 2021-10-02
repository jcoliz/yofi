using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        }
    }
}
