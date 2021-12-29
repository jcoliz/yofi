using Common.ChartJS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YoFi.Tests.Pages
{
    [TestClass]
    public class ChartJSTest
    {
        private static ChartColor Red = new ChartColor(1, 0, 0, 1.0);
        private static ChartColor Green = new ChartColor(0, 1, 0, 1.0);
        private static ChartColor Blue  = new ChartColor(0, 0, 1, 1.0);

        private static List<ChartColor> ThreeColors;

        [TestInitialize]
        public void SetUp()
        {
            ThreeColors = new List<ChartColor>() { Red, Green, Blue };
        }

        [TestMethod]
        public void CreatePieChart()
        {
            // Given: Some data
            var count = ChartConfig.MaxSegments * 2;
            var data = Enumerable.Range(0, count).Select(x => (x.ToString(),x));

            // When: Creating a pie cahrt from it
            var result = ChartConfig.CreatePieChart(data, ThreeColors);

            // Then: It looks about right
            Assert.AreEqual("doughnut", result.Type);
            Assert.AreEqual(1, result.Data.Datasets.Count());
            Assert.AreEqual(ChartConfig.MaxSegments, result.Data.Labels.Count());
            Assert.AreEqual(ChartConfig.MaxSegments, result.Data.Datasets.First().Data.Count());

            var expectedvalues = Enumerable.Range(0, ChartConfig.MaxSegments - 1);
            Assert.IsTrue(expectedvalues.SequenceEqual(result.Data.Datasets.First().Data.Take(ChartConfig.MaxSegments - 1)));

            var expectedlabels = Enumerable.Range(0, ChartConfig.MaxSegments - 1).Select(x => x.ToString());
            Assert.IsTrue(expectedlabels.SequenceEqual(result.Data.Labels.Take(ChartConfig.MaxSegments - 1)));
        }
        [TestMethod]
        public void CreateBarChart()
        {
            // Given: Some data
            var count = ChartConfig.MaxSegments * 2;
            var data = Enumerable.Range(0, count).Select(x => (x.ToString(), x));

            // When: Creating a pie cahrt from it
            var result = ChartConfig.CreateBarChart(data, ThreeColors);

            // Then: It looks about right
            Assert.AreEqual("bar", result.Type);
            Assert.AreEqual(1, result.Data.Datasets.Count());
            Assert.AreEqual(ChartConfig.MaxSegments, result.Data.Labels.Count());
            Assert.AreEqual(ChartConfig.MaxSegments, result.Data.Datasets.First().Data.Count());

            var expectedvalues = Enumerable.Range(0, ChartConfig.MaxSegments - 1);
            Assert.IsTrue(expectedvalues.SequenceEqual(result.Data.Datasets.First().Data.Take(ChartConfig.MaxSegments - 1)));

            var expectedlabels = Enumerable.Range(0, ChartConfig.MaxSegments - 1).Select(x => x.ToString());
            Assert.IsTrue(expectedlabels.SequenceEqual(result.Data.Labels.Take(ChartConfig.MaxSegments - 1)));
        }
    }
}
