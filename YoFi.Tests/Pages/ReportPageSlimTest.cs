using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
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
            pagemodel = new ReportModel(engine.Object);
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(pagemodel);
        }
    }
}
