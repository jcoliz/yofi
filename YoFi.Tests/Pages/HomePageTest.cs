using Common.DotNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using YoFi.AspNet.Pages;
using YoFi.Core;
using YoFi.Core.Models;

namespace YoFi.Tests.Pages
{
    [TestClass]
    public class HomePageTest
    {
        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task OnGet(bool empty)
        {
            // Given: A blank model page
            var page = new HomeModel(new DemoConfig());

            // When: Calling Get

            var identity = new Mock<IIdentity>();
            identity.Setup(x => x.IsAuthenticated).Returns(false);
            page.PageContext.HttpContext = new DefaultHttpContext() { User = new ClaimsPrincipal(identity.Object) };

            var dbadmin = new Mock<IDataAdminProvider>();
            var dbstatus = new Mock<IDataStatus>();
            dbstatus.Setup(x => x.NumTransactions).Returns(empty ? 0 : 100);
            dbadmin.Setup(x => x.GetDatabaseStatus()).Returns(Task.FromResult(dbstatus.Object));
            await page.OnGetAsync(dbadmin.Object);

            // Then: Nothing goes wrong
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void Demo(bool isdemo)
        {
            var page = new HomeModel(new DemoConfig() { IsEnabled = isdemo });

            Assert.AreEqual(isdemo, page.isDemo);
        }
    }
}
