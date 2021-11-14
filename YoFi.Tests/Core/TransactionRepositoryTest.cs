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
            repository = new TransactionRepository(context,storage:storage);            
        }

        [TestMethod]
        public void CalculateLoanSplits()
        {
            // https://www.calculator.net/amortization-calculator.html?cloanamount=200000&cloanterm=15&cinterestrate=6&printit=0&x=69&y=17
            // Loan amount: 200k
            // Term: 15 years (180mo)
            // Rate = 6.0 %/yr
            // Payment = 1687.71

            // Given: A transaction
            var item = new Transaction() { Amount = -1687.71m, Timestamp = new DateTime(2000, 9, 1) };

            // And: A json loan definition
            var loan = "{ \"type\": \"Loan\", \"pv\": 200000, \"rate\": 6, \"term\": 180, \"principal\": \"Principal\", \"interest\": \"Interest\", \"origination\": \"1/1/2000\" }";

            // When: Calculatiung loan splits
            var splits = transactionRepository.CalculateLoanSplits(item, loan);

            // Then: Two splits are created
            Assert.AreEqual(2, splits.Count());

            // And: The splits are as expected (using an excel amortization table for source)
            Assert.AreEqual(-972.01m, splits.Where(x => x.Category == "Interest").Single().Amount);
            Assert.AreEqual(-715.70m, splits.Where(x => x.Category == "Principal").Single().Amount);
        }
    }
}
