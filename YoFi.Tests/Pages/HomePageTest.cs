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
using YoFi.AspNet.Pages;
using YoFi.Core;
using YoFi.Core.Models;

namespace YoFi.Tests.Pages
{
    [TestClass]
    public class HomePageTest
    {
        [TestMethod]
        public void OnGet()
        {
            // Given: A blank model page
            var page = new HomeModel(new DemoConfig());

            // When: Calling Get

            var identity = new Mock<IIdentity>();
            identity.Setup(x => x.IsAuthenticated).Returns(false);
            page.PageContext.HttpContext = new DefaultHttpContext() { User = new ClaimsPrincipal(identity.Object) };

            var datacontext = new Mock<IDataContext>();
            datacontext.Setup(x => x.Transactions).Returns(Enumerable.Empty<Transaction>().AsQueryable());
            page.OnGet(datacontext.Object);

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
