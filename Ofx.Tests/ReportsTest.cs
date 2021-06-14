using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxWeb.Asp.Controllers.Helpers;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ofx.Tests
{
    [TestClass]
    public class ReportsTest
    {
        class Item : IReportable
        {
            public decimal Amount { get; set; }

            public DateTime Timestamp { get; set; }

            public string Category { get; set; }
        }

        [TestMethod]
        public void OneItem()
        {
            var report = new Report();

            var items = new List<Item>();
            var expected = new Item() { Amount = 100, Timestamp = new DateTime(DateTime.Now.Year, 01, 01), Category = "Name" };
            items.Add(expected);

            report.Build(items.AsQueryable());

            var row = report.RowLabels.Where(x => x.Name == expected.Category).SingleOrDefault();
            var col = report.ColumnLabels.Where(x => x.Name == "Jan").SingleOrDefault();

            Assert.AreEqual(2, report.RowLabels.Count());
            Assert.IsNotNull(row);
            Assert.IsNotNull(report.RowLabels.Where(x => x.IsTotal).SingleOrDefault());
            Assert.IsNotNull(col);
            Assert.IsNotNull(report.ColumnLabels.Where(x => x.IsTotal).SingleOrDefault());
            Assert.AreEqual(expected.Amount, report[col, row]);
        }
    }
}
