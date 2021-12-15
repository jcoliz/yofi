using Common.DotNet.Test;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.AspNet.Pages;
using YoFi.Core.Reports;

namespace YoFi.Tests.Pages
{
    [TestClass]
    public class ReportPageSlimTest
    {
        private ReportModel pagemodel;

        [TestInitialize]
        public void SetUp()
        {
            var engine = new Mock<IReportEngine>();
            engine.Setup(x => x.Build(It.IsAny<ReportParameters>())).Returns((ReportParameters p) => new Report() { Name = p.id, Source = Enumerable.Empty<NamedQuery>() });
            engine.Setup(x => x.Definitions).Returns(new List<ReportDefinition>() { new ReportDefinition() { Name = "Mock" } });
            pagemodel = new ReportModel(engine.Object);
        }

        [TestMethod]
        public void Empty()
        {
            // When: Creating a new pagemodel
            // (Established on initialize)

            // Then: The object is valid
            Assert.IsNotNull(pagemodel);
        }

        [TestMethod]
        public async Task EmptyParameters()
        {
            // When: Getting the page with empty parameters
            var parameters = new ReportParameters();
            var actionresult = await pagemodel.OnGetAsync(parameters);
            Assert.That.IsOfType<PageResult>(actionresult);

            // Then: The 'all' report is built, which is the default
            Assert.AreEqual("all", pagemodel.Report.Name);
        }

        [TestMethod]
        public async Task SetId()
        {
            // When: Getting the page with a certain {name} report
            var name = "Empty";
            var parameters = new ReportParameters() { id = name };
            var actionresult = await pagemodel.OnGetAsync(parameters);
            Assert.That.IsOfType<PageResult>(actionresult);

            // Then: The expected {name} report is built
            Assert.AreEqual(name, pagemodel.Report.Name);
        }

        [TestMethod]
        public async Task SetMonth12()
        {
            // Given: It is now certain {year}
            var now = 2020;

            // When: Getting the page with a report for a previous year
            var parameters = new ReportParameters() { year = now - 1 };
            pagemodel.Now = new DateTime(now,1,1);
            var actionresult = await pagemodel.OnGetAsync(parameters);
            Assert.That.IsOfType<PageResult>(actionresult);

            // Then: The page is showing all 12 months
            // because we've asked for a prior year
            Assert.AreEqual(12, pagemodel.Parameters.month);
        }

        [TestMethod]
        public void Definitions()
        {
            // Given: A single definition named "Mock"
            // (Established on initialize)

            // When: Inspecting the definitions
            IReportNavbarViewModel viewModel = pagemodel;
            var definitions = viewModel.Definitions;

            // Then: There is a single definition named "Mock"
            Assert.AreEqual(1, definitions.Count());
            Assert.AreEqual("Mock", definitions.First().Name);
        }

        [TestMethod]
        public async Task NotFound()
        {
            // Given: A report engine where all definitions will throw KeyNotFound
            var engine = new Mock<IReportEngine>();
            engine.Setup(x => x.Build(It.IsAny<ReportParameters>())).Returns((ReportParameters p) => throw new KeyNotFoundException());
            pagemodel = new ReportModel(engine.Object);

            // When: Getting the page while asking for a report defintiion that doesn't exist
            var parameters = new ReportParameters() { id = "dontexist" };
            var actionresult = await pagemodel.OnGetAsync(parameters);

            // Then: Result is Not Found
            Assert.That.IsOfType<NotFoundObjectResult>(actionresult);
        }

        [TestMethod]
        public async Task SetMonth12UsingSession()
        {
            // Given: It is now certain {year}
            var year = 2019;
            pagemodel.Now = new DateTime(year, 1, 1);

            // And: The "Year" set in the session is a previous year
            var sessionyear = year - 1;
            var httpcontext = new DefaultHttpContext();
            var session = new Mock<ISession>();
            var bytes = UTF8Encoding.UTF8.GetBytes(sessionyear.ToString());
            session.Setup(x => x.TryGetValue("Year", out bytes)).Returns(true);
            httpcontext.Session = session.Object;
            pagemodel.PageContext.HttpContext = httpcontext;

            // When: Getting the page with blank parameters
            var parameters = new ReportParameters();
            var actionresult = await pagemodel.OnGetAsync(parameters);
            Assert.That.IsOfType<PageResult>(actionresult);

            // Then: The page is showing all 12 months
            // because we've asked for a prior year
            Assert.AreEqual(12, pagemodel.Parameters.month);
        }
    }
}
