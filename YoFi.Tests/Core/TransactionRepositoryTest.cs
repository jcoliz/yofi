using Common.NET.Test;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class TransactionRepositoryTest : BaseRepositoryTest<Transaction>
    {
        protected override List<Transaction> Items
        {
            get
            {
                // Need to make a new one every time we ask for it, because the old items
                // tracked IDs for a previous test
                return new List<Transaction>()
                {
                    new Transaction() { ID = 1, Category = "B", Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, BankReference = "C" },
                    new Transaction() { ID = 2, Category = "A", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m, BankReference = "D" },
                    new Transaction() { ID = 3, Category = "C", Payee = "5", Timestamp = new DateTime(DateTime.Now.Year, 01, 01), Amount = 300m, BankReference = "B" },
                    new Transaction() { ID = 4, Category = "B", Payee = "1", Timestamp = new DateTime(DateTime.Now.Year, 01, 05), Amount = 400m, BankReference = "E" },
                    new Transaction() { ID = 5, Category = "B", Payee = "4", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 500m, BankReference = "A" },
                    new Transaction() { Category = "B", Payee = "34", Memo = "222", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 500m, BankReference = "J" },
                    new Transaction() { Category = "B", Payee = "1234", Memo = "Wut", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 500m, BankReference = "I" },
                    new Transaction() { Category = "C", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m, BankReference = "G" },
                    new Transaction() { Category = "ABC", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m, BankReference = "H" },
                    new Transaction() { Category = "DE:CAF", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m, BankReference = "F" },
                    new Transaction() { Category = "GH:CAF", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "DE:RGB", Payee = "2", Memo = "CAFE", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "GH:RGB", Payee = "2", Memo = "CONCACAF", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "GH:XYZ", Payee = "2", Memo = "4", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "GH:RGB", Payee = "2", Memo = "Wut", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "DE:RGB", Payee = "CAFE", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "GH:RGB", Payee = "CONCACAF", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "GH:XYZ", Payee = "4", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "GH:RGB", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Amount = 123m },
                    new Transaction() { Amount = 1.23m },
                    new Transaction() { Memo = "123" },
                };
            }
        }

        protected override int CompareKeys(Transaction x, Transaction y) => x.Payee.CompareTo(y.Payee);

        private ITransactionRepository transactionRepository => repository as ITransactionRepository;

        [TestInitialize]
        public void SetUp()
        {
            context = new MockDataContext();
            var storage = new TestAzureStorage();
            repository = new TransactionRepository(context, storage:storage);            
        }

        public static IEnumerable<object[]> CalculateLoanSplits_Data =>
            new[]
            {
                new object[] { 1,-1000m,-687.71m },
                new object[] { 2,-996.56m,-691.15m },
                new object[] { 3,-993.11m,-694.6m },
                new object[] { 4,-989.63m,-698.08m },
                new object[] { 5,-986.14m,-701.57m },
                new object[] { 6,-982.63m,-705.08m },
                new object[] { 7,-979.11m,-708.6m },
                new object[] { 8,-975.57m,-712.14m },
                new object[] { 9,-972.01m,-715.7m },
                new object[] { 10,-968.43m,-719.28m },
                new object[] { 20,-931.64m,-756.07m },
                new object[] { 21,-927.86m,-759.85m },
                new object[] { 34,-876.96m,-810.75m },
                new object[] { 53,-796.37m,-891.34m },
                new object[] { 70,-717.5m,-970.21m },
                new object[] { 90,-615.73m,-1071.98m },
                new object[] { 111,-497.36m,-1190.35m },
                new object[] { 133,-359.32m,-1328.39m },
                new object[] { 148,-256.12m,-1431.59m },
                new object[] { 180,-8.4m,-1679.31m },
            };

        [DynamicData(nameof(CalculateLoanSplits_Data))]
        [DataTestMethod]
        public void CalculateLoanSplits(int inmonth, decimal interest, decimal principal)
        {
            // https://www.calculator.net/amortization-calculator.html?cloanamount=200000&cloanterm=15&cinterestrate=6&printit=0&x=69&y=17
            // Loan amount: 200k
            // Term: 15 years (180mo)
            // Rate = 6.0 %/yr
            // Payment = 1687.71

            // Given: A transaction

            var year = 2000 + (inmonth-1) / 12;
            var month = 1 + (inmonth-1) % 12;

            var item = new Transaction() { Amount = -1687.71m, Timestamp = new DateTime(year, month, 1) };

            // And: A loan rule definition
            var rule = "Mortgage Principal [Loan] { \"interest\": \"Mortgage Interest\", \"amount\": 200000, \"rate\": 6, \"term\": 180, \"origination\": \"1/1/2000\" } ";

            // When: Calculatiung loan splits
            var splits = transactionRepository.CalculateCustomSplitRules(item, rule);

            // Then: Two splits are created
            Assert.AreEqual(2, splits.Count());

            // And: The splits are as expected (using an excel amortization table for source)
            Assert.AreEqual(interest, splits.Where(x => x.Category == "Mortgage Interest").Single().Amount);
            Assert.AreEqual(principal, splits.Where(x => x.Category == "Mortgage Principal").Single().Amount);
        }
    }
}
