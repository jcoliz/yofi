using Common.DotNet.Test;
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
            engine.Setup(x => x.Build(It.IsAny<ReportParameters>())).Returns((ReportParameters p) => new Report() { Name = p.id });
            engine.Setup(x => x.Definitions).Returns(new List<ReportDefinition>() { new ReportDefinition() { Name = "Mock" } });
            pagemodel = new ReportModel(engine.Object);
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(pagemodel);
        }

        [TestMethod]
        public async Task EmptyParameters()
        {
            var parameters = new ReportParameters();
            var actionresult = await pagemodel.OnGetAsync(parameters);

            Assert.That.IsOfType<PageResult>(actionresult);
            Assert.AreEqual("all", pagemodel.Report.Name);
        }

        [TestMethod]
        public async Task SetId()
        {
            var name = "Empty";
            var parameters = new ReportParameters() { id = name };
            var actionresult = await pagemodel.OnGetAsync(parameters);

            Assert.That.IsOfType<PageResult>(actionresult);
            Assert.AreEqual(name, pagemodel.Report.Name);
        }

        [TestMethod]
        public async Task SetMonth12()
        {
            var now = 2020;
            var parameters = new ReportParameters() { year = now - 1 };
            pagemodel.Now = new DateTime(now,1,1);
            var actionresult = await pagemodel.OnGetAsync(parameters);

            Assert.That.IsOfType<PageResult>(actionresult);
            Assert.AreEqual(12, pagemodel.Parameters.month);
        }

        [TestMethod]
        public void Definitions()
        {
            IReportNavbarViewModel viewModel = pagemodel;
            var definitions = viewModel.Definitions;

            Assert.AreEqual(1, definitions.Count());
            Assert.AreEqual("Mock", definitions.First().Name);
        }

        [TestMethod]
        public async Task NotFound()
        {
            var engine = new Mock<IReportEngine>();
            engine.Setup(x => x.Build(It.IsAny<ReportParameters>())).Returns((ReportParameters p) => throw new KeyNotFoundException());
            pagemodel = new ReportModel(engine.Object);

            var parameters = new ReportParameters();
            var actionresult = await pagemodel.OnGetAsync(parameters);

            Assert.That.IsOfType<NotFoundObjectResult>(actionresult);
        }
    }
}
