using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxWeb.Asp.Controllers.Helpers;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System;
using System.Collections;
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

        public class ReportSeries : IGrouping<string, IReportable>
        {
            public string Key { get; set; }

            public IEnumerable<IReportable> Items { get; set; }

            public IEnumerator<IReportable> GetEnumerator() => Items.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
        }

        List<Item> Items;

        Report report = null;


        void DoBuild(IEnumerable<Item> these, int fromlevel = 0, int numlevels = 1)
        {
            report.Build(these.AsQueryable(), fromlevel, numlevels);
            report.WriteToConsole();
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
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 01, 01), Category = "Name" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 01, 01), Category = "Name" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 02, 01), Category = "Name" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 02, 01), Category = "Name" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 03, 01), Category = "Name" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 02, 01), Category = "Other" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 02, 01), Category = "Other" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 03, 01), Category = "Other" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 04, 01), Category = "Other" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 04, 01), Category = "Other:Something:A" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 04, 01), Category = "Other:Something:A" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 05, 01), Category = "Other:Something:A" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 06, 01), Category = "Other:Something:B" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 06, 01), Category = "Other:Else" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 06, 01), Category = "Other:Else" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 07, 01), Category = "Other:Else:X" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 08, 01), Category = "Other:Else:X" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 08, 01), Category = "Other:Else:Y" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 08, 01), Category = "Other:Else:Y" });
            Items.Add(new Item() { Amount = 100, Timestamp = new DateTime(2000, 06, 01), Category = "Name" });
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(report);
        }

        [TestMethod]
        public void OneItemCols()
        {
            report.WithMonthColumns = true;
            DoBuild(Items.Take(1));

            var Name = GetRow(x => x.Name == "Name");
            var Jan = GetColumn(x => x.Name == "Jan");

            Assert.AreEqual(2, report.RowLabels.Count());
            Assert.AreEqual(100, report[Jan, Name]);
        }
        [TestMethod]
        public void ThreeMonthsCols()
        {
            report.WithMonthColumns = true;
            DoBuild(Items.Take(5));

            var Name = GetRow(x => x.Name == "Name");
            var Feb = GetColumn(x => x.Name == "Feb");

            Assert.AreEqual(2, report.RowLabels.Count());
            Assert.AreEqual(4, report.ColumnLabels.Count());
            Assert.AreEqual(200m, report[Feb, Name]);
            Assert.AreEqual(500m, report[report.TotalColumn, Name]);
        }
        [TestMethod]
        public void TwoCategoriesCols()
        {
            report.WithMonthColumns = true;
            DoBuild(Items.Take(9));

            var Other = GetRow(x => x.Name == "Other");
            var Feb = GetColumn(x => x.Name == "Feb");

            Assert.AreEqual(3, report.RowLabels.Count());
            Assert.AreEqual(5, report.ColumnLabels.Count());
            Assert.AreEqual(200m, report[Feb, Other]);
            Assert.AreEqual(400m, report[report.TotalColumn, Other]);
            Assert.AreEqual(900m, report[report.TotalColumn, report.TotalRow]);
        }
        [TestMethod]
        public void SubCategoriesCols()
        {
            report.WithMonthColumns = true;
            DoBuild(Items.Skip(5).Take(8));

            var Other = GetRow(x => x.Name == "Other");
            var Apr = GetColumn(x => x.Name == "Apr");

            Assert.AreEqual(2, report.RowLabels.Count());
            Assert.AreEqual(6, report.ColumnLabels.Count());
            Assert.AreEqual(300m, report[Apr, Other]);
            Assert.AreEqual(800m, report[report.TotalColumn, Other]);
            Assert.AreEqual(800m, report[report.TotalColumn, report.TotalRow]);
        }
        [TestMethod]
        public void Simple()
        {
            DoBuild(Items.Take(13));

            var Name = GetRow(x => x.Name == "Name");
            var Other = GetRow(x => x.Name == "Other");

            Assert.AreEqual(3, report.RowLabels.Count());
            Assert.AreEqual(1, report.ColumnLabels.Count());
            Assert.AreEqual(500m, report[report.TotalColumn, Name]);
            Assert.AreEqual(800m, report[report.TotalColumn, Other]);
            Assert.AreEqual(1300m, report[report.TotalColumn, report.TotalRow]);
        }
        [TestMethod]
        public void SubItems()
        {
            DoBuild(Items.Skip(9).Take(10));

            var Other = GetRow(x => x.Name == "Other");

            Assert.AreEqual(2, report.RowLabels.Count());
            Assert.AreEqual(1, report.ColumnLabels.Count());
            Assert.AreEqual(1000m, report[report.TotalColumn, Other]);
            Assert.AreEqual(1000m, report[report.TotalColumn, report.TotalRow]);
        }
        [TestMethod]
        public void SubItemsDeep()
        {
            DoBuild(Items.Skip(9).Take(10), fromlevel: 0, numlevels: 2);

            var Other = GetRow(x => x.Name == "Other" && x.Level == 1);
            var Something = GetRow(x => x.Name == "Something" && x.Level == 0);
            var Else = GetRow(x => x.Name == "Else" && x.Level == 0);

            Assert.AreEqual(4, report.RowLabels.Count());
            Assert.AreEqual(1, report.ColumnLabels.Count());
            Assert.AreEqual(1000m, report[report.TotalColumn, Other]);
            Assert.AreEqual(400m, report[report.TotalColumn, Something]);
            Assert.AreEqual(600m, report[report.TotalColumn, Else]);
            Assert.AreEqual(1000m, report[report.TotalColumn, report.TotalRow]);
        }
        [TestMethod]
        public void SubItemsAllDeep()
        {
            DoBuild(Items.Take(19), fromlevel: 0, numlevels: 2);

            var Name = GetRow(x => x.Name == "Name" && x.Level == 1);
            var Other = GetRow(x => x.Name == "Other" && x.Level == 1);
            var Something = GetRow(x => x.Name == "Something" && x.Level == 0);
            var Else = GetRow(x => x.Name == "Else" && x.Level == 0);

            Assert.AreEqual(7, report.RowLabels.Count());
            Assert.AreEqual(1, report.ColumnLabels.Count());
            Assert.AreEqual(500m, report[report.TotalColumn, Name]);
            Assert.AreEqual(1400m, report[report.TotalColumn, Other]);
            Assert.AreEqual(400m, report[report.TotalColumn, Something]);
            Assert.AreEqual(600m, report[report.TotalColumn, Else]);
            Assert.AreEqual(1900m, report[report.TotalColumn, report.TotalRow]);
        }
        [TestMethod]
        public void SubItemsAllDeepCols()
        {
            report.WithMonthColumns = true;
            DoBuild(Items.Take(20), fromlevel: 0, numlevels: 2);

            var Name = GetRow(x => x.Name == "Name" && x.Level == 1);
            var Other = GetRow(x => x.Name == "Other" && x.Level == 1);
            var Something = GetRow(x => x.Name == "Something" && x.Level == 0);
            var Else = GetRow(x => x.Name == "Else" && x.Level == 0);
            var Jun = GetColumn(x => x.Name == "Jun");

            Assert.AreEqual(7, report.RowLabels.Count());
            Assert.AreEqual(9, report.ColumnLabels.Count());
            Assert.AreEqual(600m, report[report.TotalColumn, Name]);
            Assert.AreEqual(1400m, report[report.TotalColumn, Other]);
            Assert.AreEqual(400m, report[report.TotalColumn, Something]);
            Assert.AreEqual(600m, report[report.TotalColumn, Else]);
            Assert.AreEqual(2000m, report[report.TotalColumn, report.TotalRow]);
            Assert.AreEqual(400m, report[Jun, report.TotalRow]);
            Assert.AreEqual(200m, report[Jun, Else]);
        }
        [TestMethod]
        public void SubItemsFromL1()
        {
            DoBuild(Items.Skip(9).Take(10), fromlevel: 1, numlevels: 1);

            var Something = GetRow(x => x.Name == "Something" && x.Level == 0);
            var Else = GetRow(x => x.Name == "Else" && x.Level == 0);

            Assert.AreEqual(3, report.RowLabels.Count());
            Assert.AreEqual(1, report.ColumnLabels.Count());
            Assert.AreEqual(400m, report[report.TotalColumn, Something]);
            Assert.AreEqual(600m, report[report.TotalColumn, Else]);
            Assert.AreEqual(1000m, report[report.TotalColumn, report.TotalRow]);
        }
        [TestMethod]
        public void SubItemsFromL1Cols()
        {
            report.WithMonthColumns = true;
            DoBuild(Items.Skip(9).Take(10), fromlevel: 1, numlevels: 2);

            var Something = GetRow(x => x.Name == "Something" && x.Level == 1);
            var Else = GetRow(x => x.Name == "Else" && x.Level == 1);
            var A = GetRow(x => x.Name == "A" && x.Level == 0);
            var B = GetRow(x => x.Name == "B" && x.Level == 0);
            var Jun = GetColumn(x => x.Name == "Jun");

            Assert.AreEqual(8, report.RowLabels.Count());
            Assert.AreEqual(6, report.ColumnLabels.Count());
            Assert.AreEqual(400m, report[report.TotalColumn, Something]);
            Assert.AreEqual(300m, report[report.TotalColumn, A]);
            Assert.AreEqual(600m, report[report.TotalColumn, Else]);
            Assert.AreEqual(1000m, report[report.TotalColumn, report.TotalRow]);
            Assert.AreEqual(300m, report[Jun, report.TotalRow]);
            Assert.AreEqual(100m, report[Jun, B]);
        }
        [TestMethod]
        public void ThreeLevelsDeepAllCols()
        {
            report.WithMonthColumns = true;
            DoBuild(Items.Take(20), fromlevel: 0, numlevels: 3);

            var Name = GetRow(x => x.Name == "Name" && x.Level == 2);
            var Other = GetRow(x => x.Name == "Other" && x.Level == 2);
            var Something = GetRow(x => x.Name == "Something" && x.Level == 1);
            var Else = GetRow(x => x.Name == "Else" && x.Level == 1);
            var A = GetRow(x => x.Name == "A" && x.Level == 0);
            var B = GetRow(x => x.Name == "B" && x.Level == 0);
            var Jun = GetColumn(x => x.Name == "Jun");

            Assert.AreEqual(12, report.RowLabels.Count());
            Assert.AreEqual(9, report.ColumnLabels.Count());
            Assert.AreEqual(600m, report[report.TotalColumn, Name]);
            Assert.AreEqual(1400m, report[report.TotalColumn, Other]);
            Assert.AreEqual(400m, report[report.TotalColumn, Something]);
            Assert.AreEqual(300m, report[report.TotalColumn, A]);
            Assert.AreEqual(600m, report[report.TotalColumn, Else]);
            Assert.AreEqual(2000m, report[report.TotalColumn, report.TotalRow]);
            Assert.AreEqual(400m, report[Jun, report.TotalRow]);
            Assert.AreEqual(100m, report[Jun, B]);
            Assert.AreEqual(200m, report[Jun, Else]);

        }
        [TestMethod]
        public void TwoSeries()
        {
            // Divide the transactios into two imbalanced partitions, each partition will be a series
            // ToList() needed to execute the index % 3 calculations NOW not later
            int index = 0;
            var seriesone = new ReportSeries() { Key = "One", Items = Items.Take(20).Where(x => index++ % 3 == 0).ToList() };
            index = 0;
            var seriestwo = new ReportSeries() { Key = "Two", Items = Items.Take(20).Where(x => index++ % 3 != 0).ToList() };

            var serieslist = new List<ReportSeries>() { seriesone, seriestwo };

            report.BuildMulti(serieslist, fromlevel: 0, numlevels: 1);
            report.WriteToConsole();

            var Name = GetRow(x => x.Name == "Name" );
            var Other = GetRow(x => x.Name == "Other" );
            var One = GetColumn(x => x.Name == "One");
            var Two = GetColumn(x => x.Name == "Two");

            Assert.AreEqual(600m, report[report.TotalColumn, Name]);
            Assert.AreEqual(1400m, report[report.TotalColumn, Other]);
            Assert.AreEqual(2000m, report[report.TotalColumn, report.TotalRow]);
            Assert.AreEqual(700m, report[One, report.TotalRow]);
            Assert.AreEqual(1300m, report[Two, report.TotalRow]);
        }
        [TestMethod]
        public void TwoSeriesDeep()
        {
            // Divide the transactios into two imbalanced partitions, each partition will be a series
            // ToList() needed to execute the index % 3 calculations NOW not later
            int index = 0;
            var seriesone = new ReportSeries() { Key = "One", Items = Items.Take(20).Where(x => index++ % 3 == 0).ToList() };
            index = 0;
            var seriestwo = new ReportSeries() { Key = "Two", Items = Items.Take(20).Where(x => index++ % 3 != 0).ToList() };

            var serieslist = new List<ReportSeries>() { seriesone, seriestwo };

            report.BuildMulti(serieslist, fromlevel: 0, numlevels: 2);
            report.WriteToConsole();

            var Name = GetRow(x => x.Name == "Name");
            var Other = GetRow(x => x.Name == "Other");
            var Else = GetRow(x => x.Name == "Else");
            var One = GetColumn(x => x.Name == "One");
            var Two = GetColumn(x => x.Name == "Two");

            Assert.AreEqual(600m, report[report.TotalColumn, Name]);
            Assert.AreEqual(1400m, report[report.TotalColumn, Other]);
            Assert.AreEqual(2000m, report[report.TotalColumn, report.TotalRow]);
            Assert.AreEqual(700m, report[One, report.TotalRow]);
            Assert.AreEqual(1300m, report[Two, report.TotalRow]);
            Assert.AreEqual(200m, report[One, Else]);
            Assert.AreEqual(400m, report[Two, Else]);
        }
        [TestMethod]
        public void TwoSeriesDeepCols()
        {
            // Divide the transactios into two imbalanced partitions, each partition will be a series
            // ToList() needed to execute the index % 3 calculations NOW not later
            int index = 0;
            var seriesone = new ReportSeries() { Key = "One", Items = Items.Take(20).Where(x => index++ % 3 == 0).ToList() };
            index = 0;
            var seriestwo = new ReportSeries() { Key = "Two", Items = Items.Take(20).Where(x => index++ % 3 != 0).ToList() };

            var serieslist = new List<ReportSeries>() { seriesone, seriestwo };

            report.WithMonthColumns = true;
            report.BuildMulti(serieslist, fromlevel: 0, numlevels: 2);
            report.WriteToConsole();

            var Name = GetRow(x => x.Name == "Name");
            var Other = GetRow(x => x.Name == "Other");
            var Else = GetRow(x => x.Name == "Else");
            var One = GetColumn(x => x.Name == "One");
            var Two = GetColumn(x => x.Name == "Two");
            var JunTwo = GetColumn(x => x.Name == "Jun Two");

            Assert.AreEqual(600m, report[report.TotalColumn, Name]);
            Assert.AreEqual(1400m, report[report.TotalColumn, Other]);
            Assert.AreEqual(2000m, report[report.TotalColumn, report.TotalRow]);
            Assert.AreEqual(700m, report[One, report.TotalRow]);
            Assert.AreEqual(1300m, report[Two, report.TotalRow]);
            Assert.AreEqual(400m, report[Two, Else]);
            Assert.AreEqual(200m, report[JunTwo, Else]);
        }
    }
}