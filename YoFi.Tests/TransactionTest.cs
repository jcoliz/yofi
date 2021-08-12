using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace YoFi.Tests
{
    [TestClass]
    public class TransactionTest
    {

        [TestMethod]
        public void PayeeFixup()
        {
            var initial =  "Ext Credit Card Debit GOOGLE *Google Store     g.co/helppay#CA USA";
            var expected = "Ext Credit Card Debit GOOGLE Google Store     gcohelppayCA USA";
            Regex rx = new Regex(@"[^\s\w\d]+");
            var actual = rx.Replace(initial, new MatchEvaluator(x => string.Empty));

            Assert.AreEqual(expected, actual);
        }

    }
}
