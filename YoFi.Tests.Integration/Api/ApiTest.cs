using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.Core;
using YoFi.Core.Models;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration.Api
{
    [TestClass]
    public class ApiTest: IntegrationTest
    {
        #region Init/Cleanup

        [ClassInitialize]
        public static void InitialSetup(TestContext tcontext)
        {
            integrationcontext = new IntegrationContext(tcontext.FullyQualifiedTestClassName);
        }

        [ClassCleanup]
        public static void FinalCleanup()
        {
            integrationcontext.Dispose();
        }

        [TestInitialize]
        public void SetUp()
        {
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"user:{integrationcontext.apiconfig.Key}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean out database
            context.Set<Transaction>().RemoveRange(context.Set<Transaction>());
            context.SaveChanges();
        }

        #endregion

        #region Tests

        [TestMethod]
        public async Task GetId()
        {
            // Given: Many items in the database
            var items = await GivenFakeDataInDatabase<Transaction>(20);
            var expected = items.First();

            // When: Calling Get with '{id}'
            var response = await client.GetAsync($"/api/{expected.ID}");
            response.EnsureSuccessStatusCode();

            // Then: Requested item returned
            var apiresult = await DeserializeAsync<Transaction>(response);
            Assert.AreEqual(expected, apiresult);
        }

        [DataRow("memo")]
        [DataRow("payee")]
        [DataRow("category")]
        [DataTestMethod]
        public async Task GetTxQAny(string property)
        {
            // Given: A mix of transactions, some with '{word}' in their {property}
            var word = "CAF";
            Func<Transaction,Transaction> func = property switch {
                "category" => (x => { x.Category = x.Category + $":{word}"; return x; }),
                "memo" => (x => { x.Memo = x.Memo + $":{word}"; return x; }),
                "payee" => (x => { x.Payee = x.Payee + $":{word}"; return x; }),
                _ => null
            };
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(100, 15, func);

            // When: Calling GetTransactions q={word}
            var response = await client.GetAsync($"/api/txi/?q={word}");
            response.EnsureSuccessStatusCode();

            // Then: Only the expected items are returned
            var apiresult = await DeserializeAsync<List<Transaction>>(response);
            Assert.IsTrue(apiresult.OrderBy(TestKeyOrder<Transaction>()).SequenceEqual(chosen));
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task GetTxQReceipt(bool with)
        {
            // Given: A mix of transactions, some with receipts, some without
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(100, 15, x => { x.ReceiptUrl = "Has"; return x; });

            // When: Calling GetTransactions q='r=1' (or r=0)
            var response = await client.GetAsync($"/api/txi/?q=r%3d{(with ? '1' : '0')}");
            response.EnsureSuccessStatusCode();

            // Then: Only the transactions with (or without) receipts are returned
            var apiresult = await DeserializeAsync<List<Transaction>>(response);

            if (with)
                Assert.IsTrue(apiresult.OrderBy(TestKeyOrder<Transaction>()).SequenceEqual(chosen));
            else
                Assert.IsTrue(apiresult.OrderBy(TestKeyOrder<Transaction>()).SequenceEqual(items.Except(chosen)));
        }

        [TestMethod]
        public async Task ClearTestTransactions()
        {
            // Given: A mix of transactions, some with __test__ marker, some without
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(10, 3, x => { x.Category += DatabaseAdministration.TestMarker; return x; });

            // When: Calling ClearTestData with id="trx"
            var response = await client.PostAsync($"/api/ClearTestData/trx", null);
            response.EnsureSuccessStatusCode();

            // ANd: Only the transactions without __test__ remain
            var actual = context.Set<Transaction>().AsNoTracking().ToList().OrderBy(TestKeyOrder<Transaction>());
            Assert.IsTrue(actual.SequenceEqual(items.Except(chosen)));
        }

        [TestMethod]
        public async Task ClearTestBudgetTxs()
        {
            // Given: A mix of transactions, some with __test__ marker, some without
            (var items, var chosen) = await GivenFakeDataInDatabase<BudgetTx>(10, 3, x => { x.Category += DatabaseAdministration.TestMarker; return x; });

            // When: Calling ClearTestData with id="trx"
            var response = await client.PostAsync($"/api/ClearTestData/budgettx", null);
            response.EnsureSuccessStatusCode();

            // ANd: Only the transactions without __test__ remain
            var actual = context.Set<BudgetTx>().AsNoTracking().ToList().OrderBy(TestKeyOrder<BudgetTx>());
            Assert.IsTrue(actual.SequenceEqual(items.Except(chosen)));
        }

        [TestMethod]
        public async Task ClearTestPayees()
        {
            // Given: A mix of transactions, some with __test__ marker, some without
            (var items, var chosen) = await GivenFakeDataInDatabase<Payee>(10, 3, x => { x.Category += DatabaseAdministration.TestMarker; return x; });

            // When: Calling ClearTestData with id="trx"
            var response = await client.PostAsync($"/api/ClearTestData/payee", null);
            response.EnsureSuccessStatusCode();

            // ANd: Only the transactions without __test__ remain
            var actual = context.Set<Payee>().AsNoTracking().ToList().OrderBy(TestKeyOrder<Payee>());
            Assert.IsTrue(actual.SequenceEqual(items.Except(chosen)));
        }


        #endregion
    }
}
