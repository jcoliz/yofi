using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Controllers.Helpers
{
    public class ReportBuilder
    {
        private readonly ApplicationDbContext _context;

        public ReportBuilder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PivotTable<Label, Label, decimal>> ThreeLevelReport(IEnumerable<IGrouping<int, ISubReportable>> outergroups, bool mapcategories = false)
        {
            var result = new PivotTable<Label, Label, decimal>();

            Dictionary<string, CategoryMap> maptable = null;
            if (mapcategories)
                maptable = await _context.CategoryMaps.ToDictionaryAsync(x => x.Category + (string.IsNullOrEmpty(x.SubCategory) ? string.Empty : "&" + x.SubCategory), x => x);

            // This crazy report is THREE levels of grouping!! Months for columns, then rows and subrows for
            // categories and subcategories

            var labeltotal = new Label() { Order = 10000, Value = "TOTAL", Emphasis = true };
            var labelempty = new Label() { Order = 9999, Value = "Blank" };


            // Step through the months, which is the outer grouping
            if (outergroups != null)
                foreach (var outergroup in outergroups)
                {
                    var month = outergroup.Key;
                    var labelcol = new Label() { Order = month, Value = new DateTime(2000, month, 1).ToString("MMM") };

                    if (outergroup.Count() > 0)
                    {
                        decimal outersum = 0.0M;

                        // Step through the categories, which is the inner grouping

                        var innergroups = outergroup.GroupBy(x => x.Category);
                        foreach (var innergroup in innergroups)
                        {
                            var sum = innergroup.Sum(x => x.Amount);

                            var labelrow = labelempty;
                            if (!string.IsNullOrEmpty(innergroup.Key))
                            {
                                labelrow = new Label() { Order = 0, Value = innergroup.Key, Emphasis = true };
                            }

                            result[labelcol, labelrow] = sum;
                            outersum += sum;

                            if (!string.IsNullOrEmpty(innergroup.Key))
                            {
                                // Step through the subcategories, which is the sub-grouping (3rd level)
                                var subgroups = innergroup.GroupBy(x => x.SubCategory);
                                foreach (var subgroup in subgroups)
                                {
                                    // Regular label values

                                    sum = subgroup.Sum(x => x.Amount);
                                    labelrow = new Label() { Order = 0, Value = innergroup.Key, SubValue = subgroup.Key ?? "-" };

                                    // Add cateogory->Key mapping

                                    if (mapcategories)
                                    {
                                        CategoryMap map = null;
                                        string key = innergroup.Key;
                                        if (maptable.ContainsKey(key))
                                            map = maptable[key];
                                        if (!string.IsNullOrEmpty(subgroup.Key))
                                        {
                                            key = innergroup.Key + "&" + subgroup.Key;
                                            if (maptable.ContainsKey(key))
                                                map = maptable[key];
                                            else
                                            {
                                                var re = new Regex("^([^\\.]*)\\.");
                                                var match = re.Match(subgroup.Key);
                                                if (match.Success && match.Groups.Count > 1)
                                                {
                                                    key = innergroup.Key + "&^" + match.Groups[1].Value;
                                                    if (maptable.ContainsKey(key))
                                                        map = maptable[key];
                                                }
                                            }
                                        }
                                        if (null != map)
                                        {
                                            labelrow.Key1 = map.Key1;

                                            bool skipkey3 = false;
                                            if (string.IsNullOrEmpty(map.Key2))
                                            {
                                                if (string.IsNullOrEmpty(subgroup.Key))
                                                {
                                                    labelrow.Key2 = innergroup.Key;
                                                }
                                                else
                                                {
                                                    labelrow.Key2 = subgroup.Key;
                                                }
                                                skipkey3 = true;
                                            }
                                            else if (map.Key2.StartsWith('^'))
                                            {
                                                var re = new Regex(map.Key2);
                                                var match = re.Match(subgroup.Key);
                                                if (match.Success && match.Groups.Count > 1)
                                                    labelrow.Key2 = match.Groups[1].Value;
                                            }
                                            else
                                                labelrow.Key2 = map.Key2;

                                            if ("-" == map.Key3 || skipkey3)
                                            {
                                                // Key3 remains blank
                                            }
                                            else if (string.IsNullOrEmpty(map.Key3))
                                            {
                                                labelrow.Key3 = subgroup.Key;
                                            }
                                            else if (map.Key3.StartsWith('^'))
                                            {
                                                var re = new Regex(map.Key3);
                                                var match = re.Match(subgroup.Key);
                                                if (match.Success && match.Groups.Count > 1)
                                                    labelrow.Key3 = match.Groups[1].Value;
                                            }
                                            else
                                                labelrow.Key3 = map.Key3;
                                        }
                                    }

                                    result[labelcol, labelrow] = sum;
                                }
                            }
                        }
                        result[labelcol, labeltotal] = outersum;
                    }
                }

            // Add totals

            foreach (var row in result.Table)
            {
                var rowsum = row.Value.Values.Sum();
                result[labeltotal, row.Key] = rowsum;
            }

            return result;
        }


        // This is a three-level report, mapped, and reconsituted by Key1/Key2/Key3
        public async Task<PivotTable<Label, Label, decimal>> FourLevelReport(IEnumerable<IGrouping<int, ISubReportable>> outergroups)
        {
            // Start with a new empty report as the result
            var result = new PivotTable<Label, Label, decimal>();

            // Run the initial report
            var report = await ThreeLevelReport(outergroups,true);

            // Collect the columns
            var columns = report.Columns;
            result.Columns = columns;

            // For each line in the initial report, collect the value by key1/key2/key3
            foreach (var label in report.Table.Keys)
            {
                var row = report.Table[label];

                // Create the mapped label
                var rowlabel = new Label() { Value = label.Key1, SubValue = label.Key2, Key3 = label.Key3 };

                // Place each of the columns
                foreach( var collabel in columns)
                {
                    // Accumulate the result
                    result[collabel, rowlabel] += row[collabel];
                }
            }

            // Next, we would need to make total rows. But let's start here for now!!

            return result;
        }


    }
}
