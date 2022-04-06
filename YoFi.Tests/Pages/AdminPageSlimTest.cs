using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.AspNet.Pages;
using YoFi.Core;
using YoFi.Core.SampleData;
using static YoFi.AspNet.Pages.AdminModel;

namespace YoFi.Tests.Pages
{
    [TestClass]
    public class AdminPageSlimTest
    {
        private Mock<ISampleDataProvider> loader;
        private Mock<IDataAdminProvider> dbadmin;
        private AdminModel page;

        [TestInitialize]
        public void SetUp()
        {
            dbadmin = new Mock<IDataAdminProvider>();
            var configoptions = new Mock<IOptions<PageConfig>>();
            var config = new PageConfig() { NoDelete = true };
            configoptions.Setup(x => x.Value).Returns(config);
            loader = new Mock<ISampleDataProvider>();
            page = new AdminModel(dbadmin.Object,configoptions.Object,loader.Object);
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(page);
        }

        [TestMethod]
        public void ConfigOK()
        {
            Assert.IsTrue(page.Config.NoDelete);
        }

        [TestMethod]
        public async Task Get()
        {
            // Given: 12 offerings
            var offering = new Mock<ISampleDataSeedOffering>();
            offering.Setup(x => x.IsAvailable).Returns(true);
            var offerings = Enumerable.Range(1, 12).Select(x => offering.Object).ToList();
            loader.Setup(x => x.ListSeedOfferingsAsync()).Returns(Task.FromResult(offerings.Cast<ISampleDataSeedOffering>()));

            // And: Database status of 2 budgettxs
            var status = new Mock<IDataStatus>();
            status.Setup(x => x.NumBudgetTxs).Returns(2);
            dbadmin.Setup(x => x.GetDatabaseStatus()).Returns(Task.FromResult(status.Object));

            // When: Getting the admin page
            await page.OnGetAsync();

            // Then: The proper counts are availble for display
            Assert.AreEqual(12,page.Offerings.Count());
            Assert.AreEqual(2,page.DatabaseStatus.NumBudgetTxs);
        }

    }
}
