using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxWeb.Asp;
using OfxWeb.Asp.Controllers;
using OfxWeb.Asp.Models;
using System.Linq;
using System.Threading.Tasks;
using Common.AspNetCore.Test;
using System;

namespace Ofx.Tests
{
    [TestClass]
    public class SplitControllerTest
    {
        private ControllerTestHelper<Split, SplitsController> helper = null;

        [TestInitialize]
        public void SetUp()
        {
            helper = new ControllerTestHelper<Split, SplitsController>();
            helper.SetUp();
            helper.controller = new SplitsController(helper.context);

            helper.Items.Add(new Split() { Category = "B", SubCategory = "A", Memo = "3", Amount = 300m });
            helper.Items.Add(new Split() { Category = "A", SubCategory = "A", Memo = "2", Amount = 200m });
            helper.Items.Add(new Split() { Category = "C", SubCategory = "A", Memo = "5", Amount = 500m });
            helper.Items.Add(new Split() { Category = "A", SubCategory = "A", Memo = "1", Amount = 100m });
            helper.Items.Add(new Split() { Category = "B", SubCategory = "B", Memo = "4", Amount = 400m });

            helper.dbset = helper.context.Splits;

            // Sample data items will use 'Name' as a unique sort idenfitier
            helper.KeyFor = (x => x.Memo);
        }
        [TestCleanup]
        public void Cleanup() => helper.Cleanup();
        [TestMethod]
        public void Empty() => helper.Empty();
        [TestMethod]
        public async Task IndexEmpty() => await helper.IndexEmpty();
        [TestMethod]
        public async Task IndexSingle() => await helper.IndexSingle();
        //[TestMethod]
        // IndexMany doesn't make sense for splits
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
        [ExpectedException(typeof(NotImplementedException))]
        public async Task Download() => await helper.Download();
        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public async Task Upload() => await helper.Upload();
        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public async Task UploadWithID() => await helper.UploadWithID();
        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public async Task UploadDuplicate() => await helper.UploadDuplicate();
    }
}
