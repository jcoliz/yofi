using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common.DotNet;

namespace YoFi.Core.Tests.Unit
{
    [TestClass]
    public class NumbersTest
    {
        [DataRow(0,"")]
        [DataRow(1,"One")]
        [DataRow(19, "Nineteen")]
        [DataRow(21, "Twenty-One")]
        [DataRow(74, "Seventy-Four")]
        [DataRow(121, "One Hundred Twenty-One")]
        [DataRow(800, "Eight Hundred")]
        [DataRow(1000, "One Thousand")]
        [DataRow(40000, "Forty Thousand")]
        [DataRow(50007, "Fifty Thousand Seven")]
        [DataRow(50107, "Fifty Thousand One Hundred Seven")]
        [DataRow(21021, "Twenty-One Thousand Twenty-One")]
        [DataRow(99999, "Ninety-Nine Thousand Nine Hundred Ninety-Nine")]
        [DataTestMethod]
        public void NumbersInt(int amount, string expected)
        {
            var actual = Numbers.ToWords(amount);
            Assert.AreEqual(expected, actual);
        }

        [DataRow(1.5, "One & 50/100")]
        [DataRow(1.01, "One & 01/100")]
        [DataRow(0.01, "Zero & 01/100")]
        [DataRow(99999.99, "Ninety-Nine Thousand Nine Hundred Ninety-Nine & 99/100")]
        [DataRow(121.50, "One Hundred Twenty-One & 50/100")]
        [DataTestMethod]
        public void NumbersDecimal(double amount, string expected)
        {
            var actual = Numbers.ToWords((decimal)amount);
            Assert.AreEqual(expected, actual);
        }
    }
}
