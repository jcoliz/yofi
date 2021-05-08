using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxWeb.Asp;
using OfxWeb.Asp.Controllers;
using OfxWeb.Asp.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Ofx.Tests
{
    [TestClass]
    public class TransactionControllerTest
    {
        private ControllerTestHelper<Transaction, TransactionsController> helper = null;

        [TestInitialize]
        public void SetUp()
        {
            helper = new ControllerTestHelper<Transaction, TransactionsController>();
            helper.SetUp();
            helper.controller = new TransactionsController(helper.context, null);

            helper.Items.Add(new Transaction() { Category = "B", SubCategory = "A", Payee = "3", Timestamp = new System.DateTime(2020, 01, 03), Amount = 100m });
            helper.Items.Add(new Transaction() { Category = "A", SubCategory = "A", Payee = "2", Timestamp = new System.DateTime(2020, 01, 04), Amount = 200m });
            helper.Items.Add(new Transaction() { Category = "C", SubCategory = "A", Payee = "5", Timestamp = new System.DateTime(2020, 01, 01), Amount = 300m });
            helper.Items.Add(new Transaction() { Category = "B", SubCategory = "A", Payee = "1", Timestamp = new System.DateTime(2020, 01, 05), Amount = 400m });
            helper.Items.Add(new Transaction() { Category = "B", SubCategory = "B", Payee = "4", Timestamp = new System.DateTime(2020, 01, 03), Amount = 500m });

            helper.dbset = helper.context.Transactions;

            // Sample data items will use 'Name' as a unique sort idenfitier
            helper.KeyFor = (x => x.Payee);
        }

        [TestCleanup]
        public void Cleanup() => helper.Cleanup();
        [TestMethod]
        public void Empty() => helper.Empty();
        [TestMethod]
        public async Task IndexEmpty() => await helper.IndexEmpty();
        [TestMethod]
        public async Task IndexSingle() => await helper.IndexSingle();
        [TestMethod]
        public async Task IndexMany() => await helper.IndexMany();
        [TestMethod]
        public async Task DetailsFound() => await helper.DetailsFound();
        [TestMethod]
        public async Task DetailsNotFound() => await helper.DetailsNotFound();
        [TestMethod]
        public async Task EditFound() => await helper.EditFound();
        [TestMethod]
        public async Task EditNotFound() => await helper.EditNotFound();
        [TestMethod]
        public async Task Create() => await helper.Create();
        [TestMethod]
        public async Task EditObjectValues() => await helper.EditObjectValues();
        [TestMethod]
        public async Task DeleteFound() => await helper.DeleteFound();
        [TestMethod]
        public async Task DeleteConfirmed() => await helper.DeleteConfirmed();
        // TODO: Fix failing tests
#if false
        [TestMethod]
        public async Task Download() => await helper.Download();
        [TestMethod]
        public async Task Upload() => await helper.Upload();
        [TestMethod]
        public async Task UploadWithID() => await helper.UploadWithID();
        [TestMethod]
        public async Task UploadDuplicate() => await helper.UploadDuplicate();
#endif
    }
}
