using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxWeb.Asp;
using OfxWeb.Asp.Controllers;
using OfxWeb.Asp.Models;
using System.Linq;
using System.Threading.Tasks;
using Common.AspNetCore.Tests;

namespace Ofx.Tests
{
    [TestClass]

    public class BudgetTxControllerTest
    {
        private ControllerTestHelper<BudgetTx, BudgetTxsController> helper = null;

        [TestInitialize]
        public void SetUp()
        {
            helper = new ControllerTestHelper<BudgetTx, BudgetTxsController>();
            helper.SetUp();
            helper.controller = new BudgetTxsController(helper.context);

            helper.Items.Add(new BudgetTx() { Timestamp = new System.DateTime(2020, 06, 01),  Category = "A", Amount = 100m });
            helper.Items.Add(new BudgetTx() { Timestamp = new System.DateTime(2020, 06, 01),  Category = "B", Amount = 200m });
            helper.Items.Add(new BudgetTx() { Timestamp = new System.DateTime(2020, 05, 01),  Category = "A", Amount = 300m });
            helper.Items.Add(new BudgetTx() { Timestamp = new System.DateTime(2020, 05, 01),  Category = "B", Amount = 400m });
            helper.Items.Add(new BudgetTx() { Timestamp = new System.DateTime(2020, 05, 01),  Category = "C", Amount = 500m });

            helper.dbset = helper.context.BudgetTxs;

            // Sample data items will use 'Name' as a unique sort idenfitier
            helper.KeyFor = (x => x.Amount.ToString());
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
        [TestMethod]
        public async Task Download() => await helper.Download();
        [TestMethod]
        public async Task Upload() => await helper.Upload();
        [TestMethod]
        public async Task UploadWithID() => await helper.UploadWithID();
        [TestMethod]
        public async Task UploadDuplicate() => await helper.UploadDuplicate();
        [TestMethod]
        public async Task UploadAddNewDuplicate()
        {
            // These items are not EXACTLY duplicates, just duplicate enough to trigger the
            // hashset equality constraint on input.

            // Start with a full set of data
            await helper.AddFiveItems();

            // Add some new items, and upload all of it.
            // I think this shows the behaviour described in
            // Product Backlog Item #769: De-dupe BudgetTxs on import
            helper.Items.Add(new BudgetTx() { Timestamp = new System.DateTime(2020, 07, 01), Category = "A", Amount = 600m });
            helper.Items.Add(new BudgetTx() { Timestamp = new System.DateTime(2020, 07, 01), Category = "B", Amount = 700m });
            helper.Items.Add(new BudgetTx() { Timestamp = new System.DateTime(2020, 07, 01), Category = "C", Amount = 800m });

            // Now upload all the items. What should happen here is that only items 1-4 (not 0) get
            // uploaded, because item 0 is already there, so it gets removed as a duplicate.
            var actual = await helper.Upload(8, 3);

            // Let's make sure all three are the new items
            var findinitial = actual.Where(x => x.Timestamp.Month == 7);

            Assert.AreEqual(3, findinitial.Count());
        }
        [TestMethod]
        public async Task UploadMinmallyDuplicate()
        {
            // These items are not EXACTLY duplicates, just duplicate enough to trigger the
            // hashset equality constraint on input.

            // Start with a full set of data
            await helper.AddFiveItems();

            // Make some changes to the amounts
            helper.Items[0].Amount = 1000m;
            helper.Items[1].Amount = 2000m;
            helper.Items[2].Amount = 3000m;

            // Upload these three. They should be rejected.
            var actual = await helper.Upload(3, 0);

        }

        // TODO: Generate next month's TXs
    }
}
