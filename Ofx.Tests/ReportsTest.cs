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

        List<Item> Items;

        [TestInitialize]
        public void SetUp()
        {
            Items = new List<Item>();
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(DateTime.Now.Year, 01, 01), Category = "Name" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(DateTime.Now.Year, 01, 01), Category = "Name" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(DateTime.Now.Year, 02, 01), Category = "Name" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(DateTime.Now.Year, 02, 01), Category = "Name" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(DateTime.Now.Year, 03, 01), Category = "Name" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(DateTime.Now.Year, 02, 01), Category = "Other" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(DateTime.Now.Year, 02, 01), Category = "Other" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(DateTime.Now.Year, 03, 01), Category = "Other" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(DateTime.Now.Year, 04, 01), Category = "Other" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(DateTime.Now.Year, 04, 01), Category = "Other:Something" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(DateTime.Now.Year, 04, 01), Category = "Other:Something" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(DateTime.Now.Year, 05, 01), Category = "Other:Something" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(DateTime.Now.Year, 06, 01), Category = "Other:Something" });
        }

        [TestMethod]
        public void OneItem()
        {
            var report = new Report();

            var testitems = Items.Take(1);
            var expected = testitems.Single();

            report.Build(testitems.AsQueryable());

            var row = report.RowLabels.Where(x => x.Name == expected.Category).SingleOrDefault();
            var col = report.ColumnLabels.Where(x => x.Name == "Jan").SingleOrDefault();

            Assert.AreEqual(2, report.RowLabels.Count());
            Assert.IsNotNull(row);
            Assert.IsNotNull(report.RowLabels.Where(x => x.IsTotal).SingleOrDefault());
            Assert.IsNotNull(col);
            Assert.IsNotNull(report.ColumnLabels.Where(x => x.IsTotal).SingleOrDefault());
            Assert.AreEqual(expected.Amount, report[col, row]);
        }
        [TestMethod]
        public void ThreeMonths()
        {
            var report = new Report();

            var testitems = Items.Take(5);

            report.Build(testitems.AsQueryable());

            var row = report.RowLabels.Where(x => x.Name == "Name").SingleOrDefault();
            var col = report.ColumnLabels.Where(x => x.Name == "Feb").SingleOrDefault();
            var totalcol = report.ColumnLabels.Where(x => x.IsTotal).SingleOrDefault();
            var totalrow = report.RowLabels.Where(x => x.IsTotal).SingleOrDefault();

            Assert.AreEqual(2, report.RowLabels.Count());
            Assert.AreEqual(4, report.ColumnLabels.Count());
            Assert.IsNotNull(row);
            Assert.IsNotNull(totalrow);
            Assert.IsNotNull(col);
            Assert.IsNotNull(totalcol);
            Assert.AreEqual(200m, report[col, row]);
            Assert.AreEqual(500m, report[totalcol, row]);
        }
        [TestMethod]
        public void TwoCategories()
        {
            var report = new Report();

            var testitems = Items.Take(9);

            report.Build(testitems.AsQueryable());

            var row = report.RowLabels.Where(x => x.Name == "Other").SingleOrDefault();
            var col = report.ColumnLabels.Where(x => x.Name == "Feb").SingleOrDefault();
            var totalcol = report.ColumnLabels.Where(x => x.IsTotal).SingleOrDefault();
            var totalrow = report.RowLabels.Where(x => x.IsTotal).SingleOrDefault();

            Assert.AreEqual(3, report.RowLabels.Count());
            Assert.AreEqual(5, report.ColumnLabels.Count());
            Assert.IsNotNull(row);
            Assert.IsNotNull(totalrow);
            Assert.IsNotNull(col);
            Assert.IsNotNull(totalcol);
            Assert.AreEqual(200m, report[col, row]);
            Assert.AreEqual(400m, report[totalcol, row]);
            Assert.AreEqual(900m, report[totalcol, totalrow]);
        }
        [TestMethod]
        public void SubCategories()
        {
            var report = new Report();

            var testitems = Items.Skip(5).Take(8);

            report.Build(testitems.AsQueryable());

            var row = report.RowLabels.Where(x => x.Name == "Other").SingleOrDefault();
            var col = report.ColumnLabels.Where(x => x.Name == "Apr").SingleOrDefault();
            var totalcol = report.ColumnLabels.Where(x => x.IsTotal).SingleOrDefault();
            var totalrow = report.RowLabels.Where(x => x.IsTotal).SingleOrDefault();

            Assert.AreEqual(2, report.RowLabels.Count());
            Assert.AreEqual(6, report.ColumnLabels.Count());
            Assert.IsNotNull(row);
            Assert.IsNotNull(totalrow);
            Assert.IsNotNull(col);
            Assert.IsNotNull(totalcol);
            Assert.AreEqual(300m, report[col, row]);
            Assert.AreEqual(800m, report[totalcol, row]);
            Assert.AreEqual(800m, report[totalcol, totalrow]);
        }
    }
}
