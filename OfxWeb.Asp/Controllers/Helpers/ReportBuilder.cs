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

            CategoryMapper maptable = null;
            if (mapcategories)
                maptable = new CategoryMapper(_context.CategoryMaps);

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
                                        var keys = maptable.KeysFor(innergroup.Key, subgroup.Key);

                                        labelrow.Key1 = keys[0];
                                        labelrow.Key2 = keys[1];
                                        labelrow.Key3 = keys[2];
                                        labelrow.Key4 = keys[3];
                                    }

                                    result[labelcol, labelrow] = sum;
                                }
                            }
                        }
                        result[labelcol, labeltotal] = outersum;
                    }
                }

            // Add totals

            foreach (var row in result.RowLabels)
            {
                var rowsum = result.Row(row).Sum();
                result[labeltotal, row] = rowsum;
            }

            return result;
        }


        // This is a three-level report, mapped, and reconsituted by Key1/Key2/Key3
        public async Task<PivotTable<Label, Label, decimal>> FourLevelReport(IEnumerable<IGrouping<int, ISubReportable>> outergroups)
        {
            // Start with a new empty report as the result
            var result = new PivotTable<Label, Label, decimal>();

            // Run the initial report
            var initial = await ThreeLevelReport(outergroups,true);

            // Collect the columns
            var columns = initial.ColumnLabels;
            foreach(var c in columns)
                result.ColumnLabels.Add(c);

            // For each line in the initial report, collect the value by key1/key2/key3
            foreach (var initialrow in initial.RowLabels)
            {
                // Not mapped? Skip!
                if (string.IsNullOrEmpty(initialrow.Key1))
                    continue;

                // Create the mapped label
                var rowlabel = new Label() { Value = initialrow.Key1, SubValue = initialrow.Key3 };
                if (string.IsNullOrEmpty(initialrow.Key3))
                    rowlabel.SubValue = "-";
                if (!string.IsNullOrEmpty(initialrow.Key2))
                    rowlabel.Value += $":{initialrow.Key2}";

                // Create the Key2-totals label
                var totalslabel = new Label() { Value = rowlabel.Value, Emphasis = true };

                // Create the Key1-totals label
                var toptotalslabel = new Label() { Value = initialrow.Key1, Emphasis = true, SuperHeading = true };

                // Place each of the columns
                foreach ( var collabel in columns )
                {
                    // Accumulate the result
                    result[collabel, rowlabel] += result[collabel,initialrow];

                    // Accumulate the key2 total
                    result[collabel, totalslabel] += result[collabel,initialrow];

                    // Accumulate the key1 total
                    result[collabel, toptotalslabel] += result[collabel,initialrow];
                }
            }

            // Next, we would need to make total rows. But let's start here for now!!

            return result;
        }
    }
}
