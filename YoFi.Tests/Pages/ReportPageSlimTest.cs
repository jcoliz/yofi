using Common.DotNet;
using Common.DotNet.Test;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
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
        //
        // This class has to deal with the complexity of how reports are generated now.
        // The flow looks like this:
        // 1. User calls /Report/{id}
        // 2. This loads the "ReportModel" page
        // 3. That page just returns a frame and a loading spinner for the user to look at
        // 4. It also modifies the parameters based on the current situation
        // 5. It then instructs the browser to fetch the "ReportPartialModel" page, using the modified parameters
        // 6. The second page builds the report and returns it to be injected into the DOM
        //

        private ReportModel outermodel;
        private ReportPartialModel pagemodel;
        private TestClock clock;

        [TestInitialize]
        public void SetUp()
        {
            clock = new TestClock();
            var engine = new Mock<IReportEngine>();
            engine.Setup(x => x.Build(It.IsAny<ReportParameters>())).Returns((ReportParameters p) => new Report() { Name = p.id, Source = Enumerable.Empty<NamedQuery>(), WithMonthColumns = p.showmonths ?? false, WithTotalColumn = !(p.id=="nototal") });
            engine.Setup(x => x.Definitions).Returns(new List<ReportDefinition>() { new ReportDefinition() { Name = "Mock" } });
            outermodel = new ReportModel(engine.Object,clock);
            pagemodel = new ReportPartialModel(engine.Object);
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
            outermodel.OnGet(parameters);
            var actionresult = await pagemodel.OnGetAsync(outermodel.Parameters);
            Assert.That.IsOfType<PartialViewResult>(actionresult);

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
            Assert.That.IsOfType<PartialViewResult>(actionresult);

            // Then: The expected {name} report is built
            // NOTE: The reason this bizarre logic works is that we are mocking the
            // reportbuilder here
            Assert.AreEqual(name, pagemodel.Report.Name);
        }

        [TestMethod]
        public async Task SetMonth12()
        {
            // Given: It is now certain {year}
            var now = 2020;

            // When: Getting the page with a report for a previous year
            var parameters = new ReportParameters() { year = now - 1 };
            clock.Now = new DateTime(now,1,1);
            outermodel.OnGet(parameters);

            var actionresult = await pagemodel.OnGetAsync(outermodel.Parameters);
            Assert.That.IsOfType<PartialViewResult>(actionresult);

            // Then: The page is showing all 12 months
            // because we've asked for a prior year
            Assert.AreEqual(12, outermodel.Parameters.month);
        }

        [TestMethod]
        public void Definitions()
        {
            // Given: A single definition named "Mock"
            // (Established on initialize)

            // When: Inspecting the definitions
            IReportNavbarViewModel viewModel = outermodel;
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
            pagemodel = new ReportPartialModel(engine.Object);

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
            clock.Now = new DateTime(year, 1, 1);

            // And: The "Year" set in the session is a previous year
            var sessionyear = year - 1;
            var httpcontext = new DefaultHttpContext();
            var session = new Mock<ISession>();
            var bytes = UTF8Encoding.UTF8.GetBytes(sessionyear.ToString());
            session.Setup(x => x.TryGetValue("Year", out bytes)).Returns(true);
            httpcontext.Session = session.Object;
            outermodel.PageContext.HttpContext = httpcontext;

            // When: Getting the page with blank parameters
            var parameters = new ReportParameters();
            outermodel.OnGet(parameters);
            var actionresult = await pagemodel.OnGetAsync(outermodel.Parameters);
            Assert.That.IsOfType<PartialViewResult>(actionresult);

            // Then: The page is showing all 12 months
            // because we've asked for a prior year
            Assert.AreEqual(12, outermodel.Parameters.month);
        }

        [TestMethod]
        public async Task MultiBarChart()
        {
            var parameters = new ReportParameters() { id = "nototal" };
            var actionresult = await pagemodel.OnGetAsync(parameters);
            Assert.That.IsOfType<PartialViewResult>(actionresult);

            Assert.IsTrue(pagemodel.ShowTopChart);
            Assert.IsTrue(pagemodel.ChartJson.Contains("bar"));
        }

        [TestMethod]
        public async Task LineChart()
        {
            var parameters = new ReportParameters() { showmonths = true };
            var actionresult = await pagemodel.OnGetAsync(parameters);
            Assert.That.IsOfType<PartialViewResult>(actionresult);

            Assert.IsTrue(pagemodel.ShowTopChart);
            Assert.IsTrue(pagemodel.ChartJson.Contains("line"));
        }
    }
}
