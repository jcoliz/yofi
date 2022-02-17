using Microsoft.VisualStudio.TestTools.UnitTesting;
using YoFi.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using YoFi.Tests.Helpers;
using YoFi.Tests.Helpers.ReportExtensions;
using YoFi.Core.Reports;

namespace YoFi.Tests.Core
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

        void WhenBuildingTheReport(bool sorted = false)
        {
            report.Build();
            WriteReportToContext(sorted);

        }

        private void WriteReportToContext(bool sorted = false)
        {
            var hash = (DataHash == 0) ? string.Empty : $"-{DataHash:X}";
            var filename = $"Report-{TestContext.TestName}{hash}.txt";
            File.Delete(filename);
            using var outstream = File.OpenWrite(filename);
            using var writer = new StreamWriter(outstream);
            report.Write(writer, sorted);
            writer.Close();
            TestContext.AddResultFile(filename);
        }

        public TestContext TestContext { get; set; }

        private int DataHash;

        [TestInitialize]
        public void SetUp()
        {
            report = new Report();

            Items = new List<Item>()
            {
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 01, 01), Category = "Name" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 01, 01), Category = "Name" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 02, 01), Category = "Name" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 02, 01), Category = "Name" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 03, 01), Category = "Name" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 02, 01), Category = "Other" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 02, 01), Category = "Other" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 03, 01), Category = "Other" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 04, 01), Category = "Other" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 04, 01), Category = "Other:Something:A" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 04, 01), Category = "Other:Something:A" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 05, 01), Category = "Other:Something:A" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 06, 01), Category = "Other:Something:B" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 06, 01), Category = "Other:Else" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 06, 01), Category = "Other:Else" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 07, 01), Category = "Other:Else:X" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 08, 01), Category = "Other:Else:X" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 08, 01), Category = "Other:Else:Y" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 08, 01), Category = "Other:Else:Y" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 06, 01), Category = "Name" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 04, 01), Category = "Other" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 08, 01), Category = "Other:Else:Y" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 08, 01), Category = "Other:Else:Y" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 07, 01), Category = "Other:Else:X" },
                new Item() { Amount = 1000, Timestamp = new DateTime(2000, 06, 01), Category = "Other:Something:B" },
                new Item() { Amount = 100, Timestamp = new DateTime(2000, 06, 01) },
            };

            DataHash = 0;
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
            report.Source = new NamedQueryList( Items.Take(1).AsQueryable() );

            // When: Building the report
            WhenBuildingTheReport();

            var Name = GetRow(x => x.Name == "Name");
            var Jan = GetColumn(x => x.Name == "Jan");

            Assert.AreEqual(2, report.RowLabels.Count());
            Assert.AreEqual(100, report[Jan, Name]);
        }
        [TestMethod]
        public void OneItemColsJson()
        {
            report.WithMonthColumns = true;
            report.Source = new NamedQueryList( Items.Take(1).AsQueryable() );

            // When: Building the report
            WhenBuildingTheReport();

            string json = report.ToJson();

            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var actual = root.EnumerateArray().First();
            var Name = actual.GetProperty("Name").GetString();
            var Jan = actual.GetProperty("ID:01").GetDecimal();

            var total = root.EnumerateArray().Last();
            var IsTotal = total.GetProperty("IsTotal").GetBoolean();
            var TotalTotal = total.GetProperty("TOTAL").GetDecimal();

            Assert.AreEqual("Name",Name);
            Assert.AreEqual(100m, Jan);
            Assert.AreEqual(true, IsTotal);
            Assert.AreEqual(100m, TotalTotal);
        }
        [TestMethod]
        public void ThreeMonthsCols()
        {
            report.WithMonthColumns = true;
            report.Source = new NamedQueryList(Items.Take(5).AsQueryable());

            // When: Building the report
            WhenBuildingTheReport();

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
            report.Source = new NamedQueryList(Items.Take(9).AsQueryable());

            // When: Building the report
            WhenBuildingTheReport();

            var Other = GetRow(x => x.Name == "Other");
            var Feb = GetColumn(x => x.Name == "Feb");

            Assert.AreEqual(3, report.RowLabels.Count());
            Assert.AreEqual(5, report.ColumnLabels.Count());
            Assert.AreEqual(200m, report[Feb, Other]);
            Assert.AreEqual(400m, report[report.TotalColumn, Other]);
            Assert.AreEqual(900m, report[report.TotalColumn, report.TotalRow]);
        }
        [TestMethod]
        public void TwoCategoriesColsCustomSimple()
        {
            var custom = new ColumnLabel()
            {
                Name = "Custom",
                UniqueID = "Z",
                Custom = x => 10000m
            };

            report.AddCustomColumn(custom);
            report.WithMonthColumns = true;
            report.Source = new NamedQueryList(Items.Take(9).AsQueryable());

            // When: Building the report
            WhenBuildingTheReport();

            var Other = GetRow(x => x.Name == "Other");
            var Feb = GetColumn(x => x.Name == "Feb");
            var Custom = GetColumn(x => x.Name == "Custom");

            Assert.AreEqual(3, report.RowLabels.Count());
            Assert.AreEqual(6, report.ColumnLabels.Count());
            Assert.AreEqual(200m, report[Feb, Other]);
            Assert.AreEqual(10000m, report[Custom, Other]);
            Assert.AreEqual(400m, report[report.TotalColumn, Other]);
            Assert.AreEqual(900m, report[report.TotalColumn, report.TotalRow]);
        }
        [TestMethod]
        public void TwoCategoriesColsCustomComplex()
        {
#pragma warning disable IDE0039 // Use local function -- Leave us alone, we need this to test ColumnLabel.Custom
            Func<Dictionary<string, decimal>, decimal> func = (cols) =>
            {
                var feb = cols["ID:02"];
                var mar = cols["ID:03"];

                return feb + mar;
            };
#pragma warning restore IDE0039 // Use local function

            var custom = new ColumnLabel()
            {
                Name = "Custom",
                UniqueID = "Z",
                Custom = func
            };

            report.AddCustomColumn(custom);
            report.WithMonthColumns = true;
            report.Source = new NamedQueryList(Items.Take(9).AsQueryable());

            // When: Building the report
            WhenBuildingTheReport();

            var Other = GetRow(x => x.Name == "Other");
            var Feb = GetColumn(x => x.Name == "Feb");
            var Custom = GetColumn(x => x.Name == "Custom");

            Assert.AreEqual(3, report.RowLabels.Count());
            Assert.AreEqual(6, report.ColumnLabels.Count());
            Assert.AreEqual(200m, report[Feb, Other]);
            Assert.AreEqual(300m, report[Custom, Other]);
            Assert.AreEqual(400m, report[report.TotalColumn, Other]);
            Assert.AreEqual(900m, report[report.TotalColumn, report.TotalRow]);
        }
        [TestMethod]
        public void SubCategoriesCols()
        {
            report.WithMonthColumns = true;
            report.Source = new NamedQueryList(Items.Skip(5).Take(8).AsQueryable());

            // When: Building the report
            WhenBuildingTheReport();

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
            report.Source = new NamedQueryList(Items.Take(13).AsQueryable());

            // When: Building the report
            WhenBuildingTheReport();

            var Name = GetRow(x => x.Name == "Name");
            var Other = GetRow(x => x.Name == "Other");

            Assert.AreEqual(3, report.RowLabels.Count());
            Assert.AreEqual(1, report.ColumnLabels.Count());
            Assert.AreEqual(500m, report[report.TotalColumn, Name]);
            Assert.AreEqual(800m, report[report.TotalColumn, Other]);
            Assert.AreEqual(1300m, report[report.TotalColumn, report.TotalRow]);

        }
        [TestMethod]
        public void NullCategory()
        {
            report.Source = new NamedQueryList(Items.Skip(25).Take(1).AsQueryable());

            // When: Building the report
            WhenBuildingTheReport();

            var Blank = GetRow(x => x.Name == "[Blank]" && !x.IsTotal);
            Assert.AreEqual(100m, report[report.TotalColumn, Blank]);
        }
        [TestMethod]
        public void SimpleJson()
        {
            report.Source = new NamedQueryList(Items.Take(13).AsQueryable());

            // When: Building the report
            WhenBuildingTheReport();

            string json = report.ToJson();

            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var Name = root.EnumerateArray().Where(x => x.GetProperty("Name").GetString() == "Name").Single();
            var Other = root.EnumerateArray().Where(x => x.GetProperty("Name").GetString() == "Other").Single();
            var Total = root.EnumerateArray().Where(x => x.GetProperty("IsTotal").GetBoolean()).Single();

            Assert.AreEqual(3, root.GetArrayLength());
            Assert.AreEqual(5, Name.EnumerateObject().Count());
            Assert.AreEqual(500m, Name.GetProperty("TOTAL").GetDecimal());
            Assert.AreEqual(800m, Other.GetProperty("TOTAL").GetDecimal());
            Assert.AreEqual(1300m, Total.GetProperty("TOTAL").GetDecimal());
        }
        [TestMethod]
        public void SimpleSorted()
        {
            report.Source = new NamedQueryList(Items.Skip(3).Take(6).AsQueryable());
            report.SortOrder = Report.SortOrders.TotalAscending;

            // When: Building the report
            WhenBuildingTheReport(sorted:true);

            var actual = report.RowLabelsOrdered;

            Assert.AreEqual("Other", actual.First().Name);
            Assert.IsTrue(actual.Last().IsTotal);
        }
        [TestMethod]
        public void SimpleSortedByName()
        {
            report.Source = new NamedQueryList(Items.Skip(3).Take(6).AsQueryable());
            report.SortOrder = Report.SortOrders.NameAscending;

            // When: Building the report
            WhenBuildingTheReport(sorted:true);

            var actual = report.RowLabelsOrdered;

            Assert.AreEqual("Name", actual.First().Name);
            Assert.IsTrue(actual.Last().IsTotal);
        }
        [TestMethod]
        public void SubItems()
        {
            report.Source = new NamedQueryList(Items.Skip(9).Take(10).AsQueryable());

            // When: Building the report
            WhenBuildingTheReport();

            var Other = GetRow(x => x.Name == "Other");

            Assert.AreEqual(2, report.RowLabels.Count());
            Assert.AreEqual(1, report.ColumnLabels.Count());
            Assert.AreEqual(1000m, report[report.TotalColumn, Other]);
            Assert.AreEqual(1000m, report[report.TotalColumn, report.TotalRow]);
        }
        [TestMethod]
        public void SubItemsDeep()
        {
            report.Source = new NamedQueryList(Items.Skip(9).Take(10).AsQueryable());
            report.NumLevels = 2;

            // When: Building the report
            WhenBuildingTheReport();

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
        public void SubItemsDeepSorted()
        {
            report.Source = new NamedQueryList(Items.Skip(9).Take(6).AsQueryable());
            report.NumLevels = 2;
            report.SortOrder = Report.SortOrders.TotalAscending;

            // When: Building the report
            WhenBuildingTheReport(sorted:true);

            var actual = report.RowLabelsOrdered.ToList();

            // Default explicit sort order is Descending by Total amount

            Assert.AreEqual("Other", actual.First().Name);
            Assert.AreEqual("Something", actual.Skip(1).First().Name); // Comes in first with 400
            Assert.AreEqual("Else", actual.Skip(2).First().Name); // Second place with 200
            Assert.IsTrue(actual.Last().IsTotal);

        }
        [TestMethod]
        public void SubItemsAllDeep()
        {
            report.Source = new NamedQueryList(Items.Take(19).AsQueryable());
            report.NumLevels = 2;

            // When: Building the report
            WhenBuildingTheReport();

            var Name = GetRow(x => x.Name == "Name" && x.Level == 1);
            var Other = GetRow(x => x.Name == "Other" && x.Level == 1);
            var Something = GetRow(x => x.Name == "Something" && x.Level == 0);
            var Else = GetRow(x => x.Name == "Else" && x.Level == 0);

            Assert.AreEqual(6, report.RowLabels.Count());
            Assert.AreEqual(1, report.ColumnLabels.Count());
            Assert.AreEqual(500m, report[report.TotalColumn, Name]);
            Assert.AreEqual(1400m, report[report.TotalColumn, Other]);
            Assert.AreEqual(400m, report[report.TotalColumn, Something]);
            Assert.AreEqual(600m, report[report.TotalColumn, Else]);
            Assert.AreEqual(1900m, report[report.TotalColumn, report.TotalRow]);
        }
        [TestMethod]
        public void SubItemsAllDeepWithBlank()
        {
            // Given: Report items with varying depth, and at least one item with no category
            report.Source = new NamedQueryList(Items.Take(26).AsQueryable());

            // When: Building a report with two levels of depth
            report.NumLevels = 2;
            WhenBuildingTheReport();

            // Then: Empty row is a top-level row

            var Blank = GetRow(x => x.Name == "[Blank]" && !x.IsTotal);

            Assert.AreEqual(1, Blank.Level);
            Assert.IsNull(Blank.Parent);
        }
        [TestMethod]
        public void SubItemsAllDeepJson()
        {
            report.Source = new NamedQueryList(Items.Take(19).AsQueryable());
            report.NumLevels = 2;

            // When: Building the report
            WhenBuildingTheReport();

            string json = report.ToJson();

            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var Name = root.EnumerateArray().Where(x => x.GetProperty("Name").GetString() == "Name").Single();
            var Other = root.EnumerateArray().Where(x => x.GetProperty("Name").GetString() == "Other").Single();
            var Something = root.EnumerateArray().Where(x => x.GetProperty("Name").GetString() == "Something").Single();
            var Else = root.EnumerateArray().Where(x => x.GetProperty("Name").GetString() == "Else").Single();
            var Total = root.EnumerateArray().Where(x => x.GetProperty("IsTotal").GetBoolean()).Single();

            Assert.AreEqual(6, root.GetArrayLength());
            Assert.AreEqual(5, Name.EnumerateObject().Count());
            Assert.AreEqual(500m, Name.GetProperty("TOTAL").GetDecimal());
            Assert.AreEqual(1400m, Other.GetProperty("TOTAL").GetDecimal());
            Assert.AreEqual(400m, Something.GetProperty("TOTAL").GetDecimal());
            Assert.AreEqual(600m, Else.GetProperty("TOTAL").GetDecimal());
            Assert.AreEqual(1900m, Total.GetProperty("TOTAL").GetDecimal());
        }
        [TestMethod]
        public void SubItemsAllDeepSorted()
        {
            report.Source = new NamedQueryList(Items.Take(24).AsQueryable());
            report.NumLevels = 2;
            report.SortOrder = Report.SortOrders.TotalAscending;

            // When: Building the report
            WhenBuildingTheReport(sorted:true);

            var actual = report.RowLabelsOrdered.ToList();

            // Default explicit sort order is Descending by Total amount

            Assert.AreEqual("Other", actual.First().Name);
            Assert.AreEqual("Else", actual.Skip(1).First().Name); // First place with 900
            Assert.AreEqual("Name", actual.Skip(4).First().Name); // Last with 600
            Assert.IsTrue(actual.Last().IsTotal);
        }
        [TestMethod]
        public void SubItemsAllThreeDeepSorted()
        {
            report.Source = new NamedQueryList(Items.Take(25).AsQueryable());
            report.NumLevels = 3;
            report.SortOrder = Report.SortOrders.TotalAscending;

            // When: Building the report
            WhenBuildingTheReport(sorted:true);

            var actual = report.RowLabelsOrdered.ToList();

            // Default explicit sort order is Descending by Total amount

            Assert.AreEqual("Other", actual.First().Name);
            Assert.AreEqual("Something", actual.Skip(1).First().Name);
            Assert.AreEqual("A", actual.Skip(3).First().Name);
            Assert.AreEqual("X", actual.Skip(6).First().Name);
            Assert.IsTrue(actual.Last().IsTotal);
        }
        [TestMethod]
        public void SubItemsAllDeepCols()
        {
            report.WithMonthColumns = true;
            report.NumLevels = 2;
            report.Source = new NamedQueryList(Items.Take(20).AsQueryable());

            // When: Building the report
            WhenBuildingTheReport();

            string json = report.ToJson();

            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var Name = root.EnumerateArray().Where(x => x.GetProperty("Name").GetString() == "Name").Single();
            var Other = root.EnumerateArray().Where(x => x.GetProperty("Name").GetString() == "Other").Single();
            var Something = root.EnumerateArray().Where(x => x.GetProperty("Name").GetString() == "Something").Single();
            var Else = root.EnumerateArray().Where(x => x.GetProperty("Name").GetString() == "Else").Single();
            var Total = root.EnumerateArray().Where(x => x.GetProperty("IsTotal").GetBoolean()).Single();

            Assert.AreEqual(6, root.GetArrayLength());
            Assert.AreEqual(13, Name.EnumerateObject().Count());
            Assert.AreEqual(600m, Name.GetProperty("TOTAL").GetDecimal());
            Assert.AreEqual(1400m, Other.GetProperty("TOTAL").GetDecimal());
            Assert.AreEqual(400m, Something.GetProperty("TOTAL").GetDecimal());
            Assert.AreEqual(600m, Else.GetProperty("TOTAL").GetDecimal());
            Assert.AreEqual(2000m, Total.GetProperty("TOTAL").GetDecimal());
            Assert.AreEqual(400m, Total.GetProperty("ID:06").GetDecimal());
            Assert.AreEqual(200m, Else.GetProperty("ID:06").GetDecimal());
        }
        [TestMethod]
        public void SubItemsAllDeepColsJson()
        {
            report.WithMonthColumns = true;
            report.NumLevels = 2;
            report.Source = new NamedQueryList(Items.Take(20).AsQueryable());

            // When: Building the report
            WhenBuildingTheReport();

            var Name = GetRow(x => x.Name == "Name" && x.Level == 1);
            var Other = GetRow(x => x.Name == "Other" && x.Level == 1);
            var Something = GetRow(x => x.Name == "Something" && x.Level == 0);
            var Else = GetRow(x => x.Name == "Else" && x.Level == 0);
            var Jun = GetColumn(x => x.Name == "Jun");

            Assert.AreEqual(6, report.RowLabels.Count());
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
            report.Source = new NamedQueryList(Items.Skip(9).Take(10).AsQueryable());
            report.SkipLevels = 1;

            // When: Building the report
            WhenBuildingTheReport();

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
            report.Source = new NamedQueryList(Items.Skip(9).Take(10).AsQueryable());
            report.SkipLevels = 1;
            report.NumLevels = 2;

            // When: Building the report
            WhenBuildingTheReport();

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
            report.Source = new NamedQueryList(Items.Take(20).AsQueryable());
            report.NumLevels = 3;

            // When: Building the report
            WhenBuildingTheReport();

            var Name = GetRow(x => x.Name == "Name" && x.Level == 2);
            var Other = GetRow(x => x.Name == "Other" && x.Level == 2);
            var Something = GetRow(x => x.Name == "Something" && x.Level == 1);
            var Else = GetRow(x => x.Name == "Else" && x.Level == 1);
            var A = GetRow(x => x.Name == "A" && x.Level == 0);
            var B = GetRow(x => x.Name == "B" && x.Level == 0);
            var Jun = GetColumn(x => x.Name == "Jun");

            Assert.AreEqual(11, report.RowLabels.Count());
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
        //[TestMethod]
        public void ThreeLevelsDeepSorted()
        {
            report.Source = new NamedQueryList(Items.Take(20).AsQueryable());
            report.NumLevels = 3;

            // When: Building the report
            WhenBuildingTheReport(sorted:true);

            var sortedrows = report.RowLabelsOrdered;
            Console.WriteLine(string.Join(',', sortedrows.Select(x => x.Name)));
        }

        [TestMethod]
        public void ThreeLevelsDeepLeafs()
        {
            report.Source = new NamedQueryList(new NamedQuery() { Query = Items.Take(20).AsQueryable(), LeafRowsOnly = true });

            // When: Building the report
            WhenBuildingTheReport();

            var Name = GetRow(x => x.Name == "Name" && x.Level == 0);
            var Other = GetRow(x => x.Name == "Other" && x.Level == 0);
            var Else = GetRow(x => x.Name == "Other:Else" && x.Level == 0);

            Assert.AreEqual(7, report.RowLabels.Count());
            Assert.AreEqual(1, report.ColumnLabels.Count());
            Assert.AreEqual(600m, report[report.TotalColumn, Name]);
            Assert.AreEqual(200m, report[report.TotalColumn, Else]);
        }


        NamedQueryList MultiSeriesSource =>new NamedQueryList()
        {
            { "One",Items.Take(20).Where(x => Items.IndexOf(x) % 3 == 0).ToList().AsQueryable() },
            { "Two",Items.Take(20).Where(x => Items.IndexOf(x) % 3 != 0).ToList().AsQueryable() }
        };

        [TestMethod]
        public void TwoSeries()
        {
            report.Source = MultiSeriesSource;

            // When: Building the report
            WhenBuildingTheReport();

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
            report.Source = MultiSeriesSource;
            report.NumLevels = 2;

            // When: Building the report
            WhenBuildingTheReport();

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
        public void TwoSeriesDeepCustomPct()
        {
            report.AddCustomColumn(
                new ColumnLabel()
                {
                    Name = "Pct",
                    UniqueID = "Z",
                    DisplayAsPercent = true,
                    Custom = (cols) => cols["ID:Two"] == 0 ? 0 : cols["ID:One"] / cols["ID:Two"]
                }
            );
            report.Source = MultiSeriesSource;
            report.NumLevels = 2;

            // When: Building the report
            WhenBuildingTheReport();

            var Name = GetRow(x => x.Name == "Name");
            var Other = GetRow(x => x.Name == "Other");
            var Else = GetRow(x => x.Name == "Else");
            var One = GetColumn(x => x.Name == "One");
            var Two = GetColumn(x => x.Name == "Two");
            var Pct = GetColumn(x => x.Name == "Pct");

            Assert.AreEqual(600m, report[report.TotalColumn, Name]);
            Assert.AreEqual(1400m, report[report.TotalColumn, Other]);
            Assert.AreEqual(0.5m, report[Pct, Name]);
            Assert.AreEqual(5m/9m, report[Pct, Other]);
            Assert.AreEqual(2000m, report[report.TotalColumn, report.TotalRow]);
            Assert.AreEqual(700m, report[One, report.TotalRow]);
            Assert.AreEqual(1300m, report[Two, report.TotalRow]);
            Assert.AreEqual(200m, report[One, Else]);
            Assert.AreEqual(400m, report[Two, Else]);
        }
        [TestMethod]
        public void TwoSeriesDeepCols()
        {
            // I'm not totally sure WHAT this report should like,
            // So, for now it's going to total all the series into single month cols

            report.WithMonthColumns = true;
            report.Source = MultiSeriesSource;
            report.NumLevels = 2;

            // When: Building the report
            WhenBuildingTheReport();

            var Name = GetRow(x => x.Name == "Name");
            var Other = GetRow(x => x.Name == "Other");
            var Else = GetRow(x => x.Name == "Else");
            var One = GetColumn(x => x.Name == "One");
            var Two = GetColumn(x => x.Name == "Two");
            var Jun = GetColumn(x => x.Name == "Jun");

            Assert.AreEqual(600m, report[report.TotalColumn, Name]);
            Assert.AreEqual(1400m, report[report.TotalColumn, Other]);
            Assert.AreEqual(2000m, report[report.TotalColumn, report.TotalRow]);
            Assert.AreEqual(700m, report[One, report.TotalRow]);
            Assert.AreEqual(1300m, report[Two, report.TotalRow]);
            Assert.AreEqual(400m, report[Two, Else]);
            Assert.AreEqual(200m, report[Jun, Else]);
            Assert.AreEqual(400m, report[Jun, report.TotalRow]);
        }

        [TestMethod]
        public void ThreeLevelsDeep_SkipLevel_IncludesBlanks_Bug1213()
        {
            // Given: Transaction source which includes categories of Top:[Blank]
            // (i.e. just "Top"), and asking for skip-levels one
            var items = Items.Take(20).Where(x=>x.Category.StartsWith("Other"));
            report.Source = new NamedQueryList(items.AsQueryable());
            report.NumLevels = 2;
            report.SkipLevels = 1;

            // When: Building the report
            WhenBuildingTheReport(sorted: true);

            // Then: The total includes all items
            var expected = items.Sum(x => x.Amount);
            Assert.AreEqual(expected, report.GrandTotal);

            // And the "ID:[Blank]" row is included
            expected = items.Where(x => x.Category == "Other").Sum(x => x.Amount);
            var Blank = GetRow(x => x.Name == "[Blank]" && !x.IsTotal);
            Assert.AreEqual(expected, report[report.TotalColumn,Blank]);
        }

        [TestMethod]
        public void Slicer_TwoSeriesDeepCols()
        {
            // User Story 1120: Summary report optimization: Run the report once,
            // then extract pieces of the report out of the master
            //
            // Here's the idea. We run the "TwoSeriesDeepCols" report. Then we ask
            // for a slice of the report starting at "Other".
            //
            // This gives the same result as if we had asked for an "Other" only report
            // from the start

            // First, let's build the EXPECTED result. This is what things SHOULD look
            // like when we're done.
            var bigitems = Items.Take(20);
            var littleitems = bigitems.Where(x => x.Category.StartsWith("Other"));
            var source = new NamedQueryList()
            {
                { "One",littleitems.Where((x,i) => i % 3 == 0).ToList().AsQueryable() },
                { "Two",littleitems.Where((x,i) => i % 3 != 0).ToList().AsQueryable() }
            };

            report.WithMonthColumns = true;
            report.Source = source;
            report.NumLevels = 2;
            report.SkipLevels = 1;
            DataHash = 0;

            WhenBuildingTheReport(sorted: true);

            // Save off the generated report for later review
            var expected = report;
            report = new Report();

            // Given: A report with two top-level categories, and three levels of depth

            // NOW, create the report we want.
            source = new NamedQueryList()
            {
                { "One",bigitems.Where((x,i) => i % 3 == 0).ToList().AsQueryable() },
                { "Two",bigitems.Where((x,i) => i % 3 != 0).ToList().AsQueryable() }
            };

            report.WithMonthColumns = true;
            report.Source = source;
            report.NumLevels = 3;
            DataHash = 1;

            WhenBuildingTheReport(sorted: true);

            // When: Asking for a slice of that report from one of the top-level categories
            var bigitemsreport = report;

            report = bigitemsreport.TakeSlice("Other");
            DataHash = 2;
            WriteReportToContext(sorted: true);

            // Then: The resulting report contains looks like a fresh report we would have created
            Assert.AreEqual(expected.RowLabelsOrdered.Count(), report.RowLabelsOrdered.Count());
            expected.RowLabelsOrdered.SequenceEqual(report.RowLabelsOrdered);
            Assert.AreEqual(expected.GrandTotal, report.GrandTotal);
        }

        [TestMethod]
        public void Slicer_TakeSliceExceptOther()
        {
            // Given: A report with multiple top categories, and levels of depth below those
            var allitems = Items.Take(19); 
            report.Source = new NamedQueryList(allitems.AsQueryable());
            report.NumLevels = 2;
            WhenBuildingTheReport(sorted:true);
            var allitemsreport = report;

            // When: Taking a slice EXCEPT one category
            report = allitemsreport.TakeSliceExcept(new string[] { "Other" });
            DataHash = 1;
            WriteReportToContext(sorted: true);
            var actual = report;

            // Then: The report looks like it would have if we'd have created it without the
            // removed categories to begin with

            var smallitems = allitems.Where(x => !x.Category.StartsWith("Other"));
            report = new Report()
            {
                Source = new NamedQueryList(smallitems.AsQueryable())
            };
            DataHash = 2;
            WhenBuildingTheReport(sorted: true);
            var expected = report;

            Assert.AreEqual(expected.RowLabelsOrdered.Count(), actual.RowLabelsOrdered.Count());
            expected.RowLabelsOrdered.SequenceEqual(actual.RowLabelsOrdered);
            Assert.AreEqual(expected.GrandTotal, actual.GrandTotal);
        }

        [TestMethod]
        public void Slicer_TakeSliceExceptName()
        {
            // Given: A report with multiple top categories, and levels of depth below those
            var allitems = Items.Take(19);
            report.Source = new NamedQueryList(allitems.AsQueryable());
            report.NumLevels = 2;
            WhenBuildingTheReport(sorted: true);
            var allitemsreport = report;

            // When: Taking a slice EXCEPT one category
            report = allitemsreport.TakeSliceExcept(new string[] { "Name" });
            DataHash = 1;
            WriteReportToContext(sorted: true);
            var actual = report;

            // Then: The report looks like it would have if we'd have created it without the
            // removed categories to begin with

            var smallitems = allitems.Where(x => !x.Category.StartsWith("Name"));
            report = new Report()
            {
                Source = new NamedQueryList(smallitems.AsQueryable())
            };
            report.NumLevels = 2;
            DataHash = 2;
            WhenBuildingTheReport(sorted: true);
            var expected = report;

            Assert.AreEqual(expected.RowLabelsOrdered.Count(), actual.RowLabelsOrdered.Count());
            expected.RowLabelsOrdered.SequenceEqual(actual.RowLabelsOrdered);
            Assert.AreEqual(expected.GrandTotal, actual.GrandTotal);
        }

        [TestMethod]
        public void Prune()
        {
            // Given: a report with multiple levels of depth
            var allitems = Items.Take(19);
            report.Source = new NamedQueryList(allitems.AsQueryable());
            report.NumLevels = 2;
            WhenBuildingTheReport(sorted: true);

            // When: Pruning it to one level
            report.PruneToLevel(1);
            var actual = report;

            // Then: The reports looks like it would if it was originally created with 
            // that level of depth
            report = new Report()
            {
                Source = new NamedQueryList(allitems.AsQueryable())
            };
            report.NumLevels = 1;
            DataHash = 1;
            WhenBuildingTheReport(sorted: true);
            var expected = report;

            Assert.AreEqual(expected.RowLabelsOrdered.Count(), actual.RowLabelsOrdered.Count());
            expected.RowLabelsOrdered.SequenceEqual(actual.RowLabelsOrdered);
            Assert.AreEqual(expected.GrandTotal, actual.GrandTotal);

        }


        [TestMethod]
        public void ManySeriesDeep()
        {
            // This crazy test creates a series for every single transaction

            report.Source = new NamedQueryList(
                Enumerable
                .Range(0, 20)
                .Select(i =>
                    new NamedQuery() 
                    { 
                        Name = i.ToString("D2"), 
                        Query = Items.Skip(i).Take(1).AsQueryable() 
                    }
                )
            );

            report.NumLevels = 2;

            // When: Building the report
            WhenBuildingTheReport();

            var Name = GetRow(x => x.Name == "Name");
            var Other = GetRow(x => x.Name == "Other");

            Assert.AreEqual(600m, report[report.TotalColumn, Name]);
            Assert.AreEqual(1400m, report[report.TotalColumn, Other]);
            Assert.AreEqual(2000m, report[report.TotalColumn, report.TotalRow]);

            for (int i = 0; i < 20; i++)
            {
                var key = i.ToString("D2");
                var Column = GetColumn(x => x.Name == key);
                Assert.AreEqual(100m, report[Column, report.TotalRow]);
            }
        }

        [DataRow("D:E:", "D,D:E,D:E:F,D:E:F:G", 100)]
        [DataRow("D:E:", "D:E", 100)]
        [DataRow("A:B:Z[^C]", "A:B:Z", 100)]
        [DataRow("A:B:Z[^C]", "A,A:X,A:B,A:B:C,A:B:D,A:B:C:X,A:B:D:X", 300)]
        [DataRow("A:B:Z[^C]", "A:B,A:B:C,A:B:D", 200)]
        [DataRow("A:B:Z[^C]", "A:B:C,A:B:D", 100)]
        [DataRow("A:B:Z[^C]", "A:B:D", 100)]
        [DataRow("D:E:F", "D,D:X,D:E:F,D:E:F:X,D:E:X", 200)]
        [DataRow("D:E", "D,D:X,D:E,D:E:X", 200)]
        [DataRow("A:B:Z[^C;D;E;F]", "A:B:X,A:B:C,A:B:E,A:B:F", 100)]
        [DataTestMethod]
        public void MixedLeafRowsAndCollector(string budget, string actual, int expected )
        {
            DataHash = HashCode.Combine(budget, actual, expected);

            report.Source = new NamedQueryList()
            {
                new NamedQuery() 
                { 
                    Name = "Budget", 
                    Query = budget.Split(',').Select(x=>new Item() { Amount = 100, Timestamp = new DateTime(2000, 01, 01), Category = x }).ToList().AsQueryable(), 
                    LeafRowsOnly = true 
                },
                new NamedQuery() 
                { 
                    Name = "Actual",
                    Query = actual.Split(',').Select(x=>new Item() { Amount = 100, Timestamp = new DateTime(2000, 01, 01), Category = x }).ToList().AsQueryable(),
                }
            };

            report.NumLevels = 3;

            // When: Building the report
            WhenBuildingTheReport();

            var Row = report.RowLabels.First();
            var Actual = GetColumn(x => x.Name == "Actual");

            // There should JUST be the budget lines
            Assert.AreEqual(budget.Split(',').Count(), report.RowLabels.Count());

            Assert.AreEqual((decimal)expected, report[Actual, Row]);
        }

        [TestMethod]
        public void FlattenLevelSingleLeafRowsOnly()
        {
            // If ONE of the series is leafrows only, this report should be flattened (all level 0).

            MixedLeafRowsAndCollector("D", "D:E:F,D,D:E,D:E:F:G", 400);

            var Row = report.RowLabels.First();

            Assert.AreEqual(0, Row.Level);
        }

        [TestMethod]
        public void CollectorMoniker()
        {
            // Task 921: Improve managed budget report NOT categories
            //
            // "A:B:^C;D;E;F" is super unwieldy and hard to read.
            //
            // Instead we're going to give this collector a MONIKER which is how it will LOOK.
            // It will still act in the old way.
            //
            // "A:B:G[^C;D;E;F]" will be the new form. It will look like "A:B:G"
            // And act like the above.

            MixedLeafRowsAndCollector("A:B:G[^C;D;E;F]", "A:B:X,A:B:C,A:B:E,A:B:F", 100);

            var Row = report.RowLabels.First();

            Assert.AreEqual("A:B:G", Row.Name);
        }

        [TestMethod]
        public void CollectorRegex()
        {
            var collectorregex = new Regex("(.*?)\\[(.*?)\\]");

            var matchme = "A:B:G[^C;D;E;F]";

            var result = collectorregex.Match(matchme);

            Assert.AreEqual(1, result.Captures.Count);
            Assert.AreEqual(3, result.Groups.Count);
            Assert.IsTrue(result.Groups.Values.Select(x=>x.Value).Contains("A:B:G"));
            Assert.IsTrue(result.Groups.Values.Select(x => x.Value).Contains("^C;D;E;F"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void CustomColumnNullFails()
        {
            report.AddCustomColumn(new ColumnLabel());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void NumLevels0Fails()
        {
            report.NumLevels = 0;
            report.Build();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void SourceNullFails()
        {
            report.Build();
        }

        [TestMethod]
        public void EmptyReportNoConsoleOut()
        {
            var sw = new StringWriter();
            sw.Close();
            report.Write(sw);

            Assert.AreEqual(0, sw.ToString().Length);
        }
    }
}