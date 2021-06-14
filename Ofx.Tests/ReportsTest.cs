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

        void DoBuild(IEnumerable<Item> these, bool flat = true, bool nocols = false)
        {
            testitems = these;
            if (flat)
                report.Build(testitems.AsQueryable(),nocols);
            else
                report.BuildTwoLevel(testitems.AsQueryable(),nocols);
            totalcol = report.ColumnLabels.Where(x => x.IsTotal).SingleOrDefault();
            totalrow = report.RowLabels.Where(x => x.IsTotal).SingleOrDefault();

            Assert.IsNotNull(totalrow);
            Assert.IsNotNull(totalcol);
        }
        void DoBuildNoCols(IEnumerable<Item> these) => DoBuild(these, nocols: true);

        void DoBuildNoColsTwoLevel(IEnumerable<Item> these) => DoBuild(these, flat: false, nocols:true);

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
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 01, 01), Category = "Name" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 01, 01), Category = "Name" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 02, 01), Category = "Name" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 02, 01), Category = "Name" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 03, 01), Category = "Name" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 02, 01), Category = "Other" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 02, 01), Category = "Other" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 03, 01), Category = "Other" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 04, 01), Category = "Other" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 04, 01), Category = "Other:Something" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 04, 01), Category = "Other:Something" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 05, 01), Category = "Other:Something" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 06, 01), Category = "Other:Something" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 06, 01), Category = "Other:Else" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 06, 01), Category = "Other:Else" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 07, 01), Category = "Other:Else" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 08, 01), Category = "Other:Else" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 08, 01), Category = "Other:Else" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 08, 01), Category = "Other:Else" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 06, 01), Category = "Name" });
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

            var Name = GetRow(x => x.Name == "Name");
            var Jan = GetColumn(x => x.Name == "Jan");

            Assert.AreEqual(2, report.RowLabels.Count());
            Assert.AreEqual(100, report[Jan, Name]);
        }
        [TestMethod]
        public void ThreeMonths()
        {
            DoBuild(Items.Take(5));

            var Name = GetRow(x => x.Name == "Name");
            var Feb = GetColumn(x => x.Name == "Feb");

            Assert.AreEqual(2, report.RowLabels.Count());
            Assert.AreEqual(4, report.ColumnLabels.Count());
            Assert.AreEqual(200m, report[Feb, Name]);
            Assert.AreEqual(500m, report[totalcol, Name]);
        }
        [TestMethod]
        public void TwoCategories()
        {
            DoBuild(Items.Take(9));

            var Other = GetRow(x => x.Name == "Other");
            var Feb = GetColumn(x => x.Name == "Feb");

            Assert.AreEqual(3, report.RowLabels.Count());
            Assert.AreEqual(5, report.ColumnLabels.Count());
            Assert.AreEqual(200m, report[Feb, Other]);
            Assert.AreEqual(400m, report[totalcol, Other]);
            Assert.AreEqual(900m, report[totalcol, totalrow]);
        }
        [TestMethod]
        public void SubCategories()
        {
            DoBuild(Items.Skip(5).Take(8));

            var Other = GetRow(x => x.Name == "Other");
            var Apr = GetColumn(x => x.Name == "Apr");

            Assert.AreEqual(2, report.RowLabels.Count());
            Assert.AreEqual(6, report.ColumnLabels.Count());
            Assert.AreEqual(300m, report[Apr, Other]);
            Assert.AreEqual(800m, report[totalcol, Other]);
            Assert.AreEqual(800m, report[totalcol, totalrow]);
        }
        [TestMethod]
        public void NoCols()
        {
            DoBuildNoCols(Items.Take(13));

            var Name = GetRow(x => x.Name == "Name");
            var Other = GetRow(x => x.Name == "Other");

            Assert.AreEqual(3, report.RowLabels.Count());
            Assert.AreEqual(1, report.ColumnLabels.Count());
            Assert.AreEqual(500m, report[totalcol, Name]);
            Assert.AreEqual(800m, report[totalcol, Other]);
            Assert.AreEqual(1300m, report[totalcol, totalrow]);
        }
        [TestMethod]
        public void NoColsSubItems()
        {
            DoBuildNoCols(Items.Skip(9).Take(10));

            var Other = GetRow(x => x.Name == "Other");

            Assert.AreEqual(2, report.RowLabels.Count());
            Assert.AreEqual(1, report.ColumnLabels.Count());
            Assert.AreEqual(1000m, report[totalcol, Other]);
            Assert.AreEqual(1000m, report[totalcol, totalrow]);
        }
        [TestMethod]
        public void NoColsSubItemsTwoLevel()
        {
            DoBuildNoColsTwoLevel(Items.Skip(9).Take(10));

            var Other = GetRow(x => x.Name == "Other" && x.Level == 1);
            var Something = GetRow(x => x.Name == "Something" && x.Level == 0);
            var Else = GetRow(x => x.Name == "Else" && x.Level == 0);

            Assert.AreEqual(4, report.RowLabels.Count());
            Assert.AreEqual(1, report.ColumnLabels.Count());
            Assert.AreEqual(1000m, report[totalcol, Other]);
            Assert.AreEqual(400m, report[totalcol, Something]);
            Assert.AreEqual(600m, report[totalcol, Else]);
            Assert.AreEqual(1000m, report[totalcol, totalrow]);
        }
        [TestMethod]
        public void NoColsSubItemsTwoLevelAll()
        {
            DoBuildNoColsTwoLevel(Items.Take(19));

            var Name = GetRow(x => x.Name == "Name" && x.Level == 1);
            var Other = GetRow(x => x.Name == "Other" && x.Level == 1);
            var Something = GetRow(x => x.Name == "Something" && x.Level == 0);
            var Else = GetRow(x => x.Name == "Else" && x.Level == 0);

            Assert.AreEqual(7, report.RowLabels.Count());
            Assert.AreEqual(1, report.ColumnLabels.Count());
            Assert.AreEqual(500m, report[totalcol, Name]);
            Assert.AreEqual(1400m, report[totalcol, Other]);
            Assert.AreEqual(400m, report[totalcol, Something]);
            Assert.AreEqual(600m, report[totalcol, Else]);
            Assert.AreEqual(1900m, report[totalcol, totalrow]);
        }
        [TestMethod]
        public void SubItemsTwoLevelAllYesCols()
        {
            DoBuild(Items.Take(20), nocols:false, flat:false);

            var Name = GetRow(x => x.Name == "Name" && x.Level == 1);
            var Other = GetRow(x => x.Name == "Other" && x.Level == 1);
            var Something = GetRow(x => x.Name == "Something" && x.Level == 0);
            var Else = GetRow(x => x.Name == "Else" && x.Level == 0);
            var Jun = GetColumn(x => x.Name == "Jun");

            Assert.AreEqual(7, report.RowLabels.Count());
            Assert.AreEqual(9, report.ColumnLabels.Count());
            Assert.AreEqual(600m, report[totalcol, Name]);
            Assert.AreEqual(1400m, report[totalcol, Other]);
            Assert.AreEqual(400m, report[totalcol, Something]);
            Assert.AreEqual(600m, report[totalcol, Else]);
            Assert.AreEqual(2000m, report[totalcol, totalrow]);
            Assert.AreEqual(400m, report[Jun, totalrow]);
            Assert.AreEqual(200m, report[Jun, Else]);
        }
    }
}
