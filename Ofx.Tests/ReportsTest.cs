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

        Report report = null;

        IEnumerable<Item> testitems;
        ColumnLabel totalcol;
        RowLabel totalrow;

        void DoBuild(IEnumerable<Item> these)
        {
            testitems = these;
            report.Build(testitems.AsQueryable());
            totalcol = report.ColumnLabels.Where(x => x.IsTotal).SingleOrDefault();
            totalrow = report.RowLabels.Where(x => x.IsTotal).SingleOrDefault();

            Assert.IsNotNull(totalrow);
            Assert.IsNotNull(totalcol);
        }

        RowLabel GetRow(Func<RowLabel, bool> predicate)
        {
            var result = report.RowLabels.Where(predicate).SingleOrDefault();

            Assert.IsNotNull(result);

            return result;
        }
        ColumnLabel GetColumn(Func<ColumnLabel, bool> predicate)
        {
            var result = report.ColumnLabels.Where(predicate).SingleOrDefault();

            Assert.IsNotNull(result);

            return result;
        }

        [TestInitialize]
        public void SetUp()
        {
            report = new Report();

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
        public void Empty()
        {
            Assert.IsNotNull(report);
        }

        [TestMethod]
        public void OneItem()
        {
            DoBuild(Items.Take(1));

            var expected = Items.First();
            var row = GetRow(x => x.Name == expected.Category);
            var col = GetColumn(x => x.Name == "Jan");

            Assert.AreEqual(2, report.RowLabels.Count());
            Assert.IsNotNull(row);
            Assert.IsNotNull(col);
            Assert.AreEqual(expected.Amount, report[col, row]);
        }
        [TestMethod]
        public void ThreeMonths()
        {
            DoBuild(Items.Take(5));

            var row = GetRow(x => x.Name == "Name");
            var col = GetColumn(x => x.Name == "Feb");

            Assert.AreEqual(2, report.RowLabels.Count());
            Assert.AreEqual(4, report.ColumnLabels.Count());
            Assert.AreEqual(200m, report[col, row]);
            Assert.AreEqual(500m, report[totalcol, row]);
        }
        [TestMethod]
        public void TwoCategories()
        {
            DoBuild(Items.Take(9));

            var row = GetRow(x => x.Name == "Other");
            var col = GetColumn(x => x.Name == "Feb");

            Assert.AreEqual(3, report.RowLabels.Count());
            Assert.AreEqual(5, report.ColumnLabels.Count());
            Assert.AreEqual(200m, report[col, row]);
            Assert.AreEqual(400m, report[totalcol, row]);
            Assert.AreEqual(900m, report[totalcol, totalrow]);
        }
        [TestMethod]
        public void SubCategories()
        {
            DoBuild(Items.Skip(5).Take(8));

            var row = GetRow(x => x.Name == "Other");
            var col = GetColumn(x => x.Name == "Apr");

            Assert.AreEqual(2, report.RowLabels.Count());
            Assert.AreEqual(6, report.ColumnLabels.Count());
            Assert.AreEqual(300m, report[col, row]);
            Assert.AreEqual(800m, report[totalcol, row]);
            Assert.AreEqual(800m, report[totalcol, totalrow]);
        }
    }
}
