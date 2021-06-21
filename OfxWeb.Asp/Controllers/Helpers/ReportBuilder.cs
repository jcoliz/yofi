using Microsoft.EntityFrameworkCore;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Controllers.Helpers
{
    public class ReportBuilder
    {
        /// <summary>
        /// Parameters used to build a report
        /// </summary>
        /// <remarks>
        /// Moved these into a class so I can make a single change in calling convention here
        /// and have it propagate out to the controller endpoints automatically
        /// </remarks>
        public class Parameters
        {
            public string id { get; set; } 
            public int? year { get; set; } 
            public int? month { get; set; } 
            public bool? showmonths { get; set; } 
            public int? level { get; set; }
        }
        private readonly ApplicationDbContext _context;

        public ReportBuilder(ApplicationDbContext context)
        {
            _context = context;
        }

        public Report BuildReport(Parameters parms)
        {
            var result = new Report();

            if (!parms.month.HasValue)
                parms.month = 12;
            if (!parms.year.HasValue)
                parms.year = DateTime.Now.Year;

            var period = new DateTime(parms.year.Value, parms.month.Value, 1);
            result.Description = $"For {parms.year.Value} through {period.ToString("MMMM")} ";

            Func<Models.Transaction, bool> inscope_t = (x => x.Timestamp.Year == parms.year && x.Hidden != true && x.Timestamp.Month <= parms.month);
            Func<Models.Split, bool> inscope_s = (x => x.Transaction.Timestamp.Year == parms.year && x.Transaction.Hidden != true && x.Transaction.Timestamp.Month <= parms.month);

            // This works around absolutely inexplicable behavior where grouping absolutely does not work without it?!
            Func<IReportable, bool> inscope_any = x => true;

            var txs = _context.Transactions.Include(x => x.Splits).Where(inscope_t).Where(x => !x.Splits.Any());
            var splits = _context.Splits.Include(x => x.Transaction).Where(inscope_s);
            var txscomplete = txs.AsQueryable<IReportable>().Concat(splits);
            var budgettxs = _context.BudgetTxs.Where(x => x.Timestamp.Year == parms.year).Where(inscope_any).AsQueryable<IReportable>();

            var excludeExpenses = new List<string>() { "Savings", "Taxes", "Income", "Transfer", "Unmapped" };
            var excludestartsExpenses = excludeExpenses.Select(x => $"{x}:").ToList();
            var txsExpenses = txs.Where(x => !excludeExpenses.Contains(x.Category) && !excludestartsExpenses.Any(y => x.Category?.StartsWith(y) ?? false));
            var splitsExpenses = splits.Where(x => !excludeExpenses.Contains(x.Category) && !excludestartsExpenses.Any(y => x.Category.StartsWith(y)));
            var txscompleteExpenses = txsExpenses.AsQueryable<IReportable>().Concat(splitsExpenses);
            var budgettxsExpenses = budgettxs.Where(x => !excludeExpenses.Contains(x.Category) && !excludestartsExpenses.Any(y => x.Category.StartsWith(y)));

            var serieslistexpenses = new List<IQueryable<IGrouping<string, IReportable>>>()
            {
                txscompleteExpenses.GroupBy(x => "Actual"),
                budgettxsExpenses.GroupBy(x => "Budget")
            };

            var serieslistall = new List<IQueryable<IGrouping<string, IReportable>>>()
            {
                txscomplete.GroupBy(x => "Actual"),
                budgettxs.GroupBy(x => "Budget")
            };

            var budgetpctcolumn = new ColumnLabel()
            {
                Name = "% Progress",
                UniqueID = "Z",
                DisplayAsPercent = true,
                Custom = (cols) =>
                {
                    if (cols.ContainsKey("ID:Budget") && cols.ContainsKey("ID:Actual"))
                        return cols["ID:Budget"] == 0 ? 0 : cols["ID:Actual"] / cols["ID:Budget"];
                    else
                        return 0;
                }
            };

            if (parms.id == "all")
            {
                result.WithMonthColumns = true;
                result.NumLevels = 2;
                result.SingleSource = txs.AsQueryable<IReportable>().Concat(splits);
                result.SortOrder = Helpers.Report.SortOrders.TotalDescending;
                result.Name = "All Transactions";
            }
            else if (parms.id == "income")
            {
                var txsI = txs.Where(x => x.Category == "Income" || x.Category.StartsWith("Income:"));
                var splitsI = splits.Where(x => x.Category == "Income" || x.Category.StartsWith("Income:"));

                result.SingleSource = txsI.AsQueryable<IReportable>().Concat(splitsI);
                result.SkipLevels = 1;
                result.DisplayLevelAdjustment = 1; // Push levels up one when displaying
                result.SortOrder = Helpers.Report.SortOrders.TotalAscending;
                result.Name = "Income";
            }
            else if (parms.id == "taxes")
            {
                var txsI = txs.Where(x => x.Category == "Taxes" || x.Category.StartsWith("Taxes:"));
                var splitsI = splits.Where(x => x.Category == "Taxes" || x.Category.StartsWith("Taxes:"));

                result.SingleSource = txsI.AsQueryable<IReportable>().Concat(splitsI);
                result.SkipLevels = 1;
                result.DisplayLevelAdjustment = 1; // Push levels up one when displaying
                result.SortOrder = Helpers.Report.SortOrders.TotalDescending;
                result.Name = "Taxes";
            }
            else if (parms.id == "savings")
            {
                var txsI = txs.Where(x => x.Category == "Savings" || x.Category.StartsWith("Savings:"));
                var splitsI = splits.Where(x => x.Category == "Savings" || x.Category.StartsWith("Savings:"));

                result.SingleSource = txsI.AsQueryable<IReportable>().Concat(splitsI);
                result.SkipLevels = 1;
                result.DisplayLevelAdjustment = 1; // Push levels up one when displaying
                result.SortOrder = Helpers.Report.SortOrders.TotalDescending;
                result.Name = "Savings";
            }
            else if (parms.id == "expenses")
            {
                result.WithMonthColumns = true;
                result.SingleSource = txsExpenses.AsQueryable<IReportable>().Concat(splitsExpenses);
                result.NumLevels = 2;
                result.SortOrder = Helpers.Report.SortOrders.TotalDescending;
                result.Name = "Expenses";
            }
            else if (parms.id == "expenses-budget")
            {
                result.SingleSource = budgettxsExpenses.AsQueryable<IReportable>();
                result.NumLevels = 3;
                result.SortOrder = Helpers.Report.SortOrders.TotalDescending;
                result.Name = "Expenses Budget";
            }
            else if (parms.id == "expenses-v-budget")
            {
                result.AddCustomColumn(budgetpctcolumn);
                result.SeriesQuerySource = serieslistexpenses;
                result.WithTotalColumn = false;
                result.NumLevels = 3;
                result.SortOrder = Helpers.Report.SortOrders.TotalDescending;
                result.Name = "Expenses vs. Budget";
            }
            else if (parms.id == "all-v-budget")
            {
                result.AddCustomColumn(budgetpctcolumn);
                result.SeriesQuerySource = serieslistall;
                result.WithTotalColumn = false;
                result.NumLevels = 3;
                result.SortOrder = Helpers.Report.SortOrders.TotalDescending;
                result.Name = "All vs. Budget";
            }
            else if (parms.id == "budget")
            {
                result.NumLevels = 3;
                result.SingleSource = budgettxs.AsQueryable<IReportable>();
                result.SortOrder = Helpers.Report.SortOrders.TotalDescending;
                result.Description = $"For {parms.year}";
                result.Name = "Full Budget";
            }
            else if (parms.id == "yoy")
            {
                // What we can't do is execute a GroupBy AFTER the Concat
                //
                // Looks like this won't be supported until 3.0.
                // https://github.com/dotnet/efcore/issues/12289
                // "However, groupby after set operation has been fixed in 3.0. However, currently we have a bug when result
                // selector is used and grouping key is part of that result selector. #18267 Workaround is to use .Select
                // rather than result selector"
                //
                // Instead, I am going to call Build TWICE with two different series lists (!!)


                var txsyoy = _context.Transactions.Include(x => x.Splits).Where(x => x.Hidden != true).Where(x => !x.Splits.Any());
                var splitsyoy = _context.Splits.Include(x => x.Transaction).Where(x => x.Transaction.Hidden != true);
                var txscompleteyoy = txsyoy.AsQueryable<IReportable>().Concat(splitsyoy);
                var serieslistyoy = txscompleteyoy.GroupBy(x => x.Timestamp.Year.ToString());

                var serieslisttxsyoy = txsyoy.GroupBy(x => x.Timestamp.Year.ToString());
                var serieslistsplitsyoy = splitsyoy.GroupBy(x => x.Transaction.Timestamp.Year.ToString());

                var years = serieslisttxsyoy.Select(x => x.Key);
                result.Description = $"For {years.Min()} to {years.Max()}";
                result.NumLevels = 3;
                result.SortOrder = Helpers.Report.SortOrders.TotalDescending;
                result.Name = "Year over Year";

                result.SeriesSource = serieslisttxsyoy;
                result.Build();
                result.SeriesSource = serieslistsplitsyoy;
            }
            else if (parms.id == "export")
            {
                result.SeriesQuerySource = serieslistall;
                result.LeafRowsOnly = true;
                result.WithTotalColumn = false;
                result.NumLevels = 4;
                result.SortOrder = Helpers.Report.SortOrders.NameAscending;
                result.Name = "Transaction Export";
            }

            if (parms.level.HasValue)
            {
                result.NumLevels = parms.level.Value;
                if (result.NumLevels == 1)
                    result.DisplayLevelAdjustment = 1;
            }

            if (parms.showmonths.HasValue)
            {
                result.WithMonthColumns = parms.showmonths.Value;
            }

            result.Build();
            result.WriteToConsole();

            return result;
        }

        public async Task<Table<Label, Label, decimal>> ThreeLevelReport(IEnumerable<IGrouping<int, ISubReportable>> outergroups, bool mapcategories = false)
        {
            var result = new Table<Label, Label, decimal>();

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
                var rowsum = result.RowValues(row).Sum();
                result[labeltotal, row] = rowsum;
            }

            return result;
        }


        // This is a three-level report, mapped, and reconsituted by Key1/Key2/Key3
        public async Task<Table<Label, Label, decimal>> FourLevelReport(IEnumerable<IGrouping<int, ISubReportable>> outergroups)
        {
            // Start with a new empty report as the result
            var result = new Table<Label, Label, decimal>();

            // Run the initial report
            var initial = await ThreeLevelReport(outergroups,true);

            // Collect the columns
            result.ColumnLabels = initial.ColumnLabels;

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
                foreach ( var collabel in initial.ColumnLabels )
                {
                    // Accumulate the result
                    result[collabel, rowlabel] += initial[collabel,initialrow];

                    // Accumulate the key2 total
                    result[collabel, totalslabel] += initial[collabel,initialrow];

                    // Accumulate the key1 total
                    result[collabel, toptotalslabel] += initial[collabel,initialrow];
                }
            }

            // Next, we would need to make total rows. But let's start here for now!!

            return result;
        }
    }
}
