using Common.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace YoFi.Core.Reports
{
    /// <summary>
    /// Build application-specific reports
    /// </summary>
    /// <remarks>
    /// This is the top-level entry point for building reports
    /// 
    /// It is where the app logic lives to arrange the data we have in our
    /// database into reports that will be interesting for the user.
    /// 
    /// Ultimately, I'd like to move the ReportDefinition objects into
    /// the database.
    /// </remarks>
    public class ReportBuilder: IReportEngine
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">Where to pull report data from</param>
        public ReportBuilder(IDataProvider context, IClock clock)
        {
            _qbuilder = new QueryBuilder(context);
            _clock = clock;
        }

        private readonly QueryBuilder _qbuilder;
        private readonly IClock _clock;

        /// <summary>
        /// Build the report from the given <paramref name="parameters"/>
        /// </summary>
        /// <param name="parameters">Parameters describing the report to be built</param>
        /// <returns>The report we built</returns>
        public Report Build(ReportParameters parameters)
        {
            var report = new Report();

            // TODO: Need a test-definable year!
            int Month = _qbuilder.Month = parameters.month ?? 12;
            int Year = _qbuilder.Year = parameters.year ?? _clock.Now.Year;

            if (!Definitions.Any(x => x.slug == parameters.slug))
                throw new KeyNotFoundException($"Unable to find report {parameters.slug}");

            var definition = Definitions.Where(x => x.slug == parameters.slug).SingleOrDefault();

            // Timeframe and description (which displays timeframe)

            report.Description = $"For {Year}";
            if (definition.WholeYear == true)
            {
                _qbuilder.Month = 12;
            }
            else
            {
                var period = new DateTime(Year, Month, 1);
                report.Description += $" through {period:MMMM} ";
            }

            // Most properties are set by report directly

            report.LoadFrom(definition);

            // Set Source

            report.Source = _qbuilder.LoadFrom(definition);

            // Removing yearprogress for now. It causes the query to be run TWICE,
            // making the report TWICE as long to run. Ugh. I will need to find a faster way.

            // WholeYear needs to be handled after source is set
            if (definition.YearProgress == true)
            {
                // What is the highest transaction in the "Actuals"?
                var actualitems = report.Source.Where(x => x.Name == "Actual");
                if (actualitems.Any() && actualitems.First().Query.Any())
                {
                    var latesttime = actualitems.First().Query.Max(x => x.Timestamp);

                    // What % of the way is it through that year?
                    report.YearProgress = (double)latesttime.DayOfYear / 365.0;
                    report.Description += $" {report.YearProgress:P0}";
                }
            }

            // Special case for yoy report

            if (parameters.slug == "yoy")
            {
                var years = report.Source.Select(x => Int32.Parse(x.Name));
                report.Description = $"For {years.Min()} to {years.Max()}";
            }

            // Set custom columns

            if (!string.IsNullOrEmpty(definition.CustomColumns))
                foreach (var col in definition.CustomColumns.Split(","))
                    report.AddCustomColumn(CustomColumnFor(col));

            // Override level based on parameters

            if (parameters.level.HasValue)
            {
                report.NumLevels = parameters.level.Value;
                if (report.NumLevels == 1)
                    report.DisplayLevelAdjustment = 1;
            }

            // Override showmonths based on parameters

            if (parameters.showmonths.HasValue)
                report.WithMonthColumns = parameters.showmonths.Value;

            // Go!

            report.Build();

            return report;
        }

        private ColumnLabel CustomColumnFor(string name)
        {
            if (name == "budgetpct")
                return new ColumnLabel()
                {
                    Name = "% Progress",
                    UniqueID = "Z",
                    DisplayAsPercent = true,
                    Custom = (cols) =>
                        cols.GetValueOrDefault("ID:Budget") == 0 || (Math.Abs(cols.GetValueOrDefault("ID:Actual") / cols.GetValueOrDefault("ID:Budget")) > 10m) ?
                            0 :
                            cols.GetValueOrDefault("ID:Actual") / cols.GetValueOrDefault("ID:Budget")
                };
            else if (name == "pctoftotal")
                return new ColumnLabel()
                {
                    Name = "% Total",
                    IsSortingAfterTotal = true,
                    DisplayAsPercent = true,
                    Custom = (cols) =>
                        (cols.GetValueOrDefault("GRANDTOTAL") == 0) ? 0 : cols.GetValueOrDefault("TOTAL") / cols.GetValueOrDefault("GRANDTOTAL")
                };
            else if (name == "budgetavailable")
                return new ColumnLabel()
                {
                    Name = "Available",
                    Custom = (cols) =>
                        cols.GetValueOrDefault("ID:Actual") - cols.GetValueOrDefault("ID:Budget")
                };
            else 
                return null;
        }

        /// <summary>
        /// Build the summary reports
        /// </summary>
        /// <remarks>
        /// These are a set of reports meant to give an overall "Income Statement" style picture of the
        /// users' income/expense life.
        /// </remarks>
        /// <param name="parms"></param>
        /// <returns></returns>
        public IEnumerable<IEnumerable<IDisplayReport>> BuildSummary(ReportParameters parms)
        {
            ReportParameters Parameters = parms;
            Parameters.slug = "summary";

            // Fixup parameter
            if (!Parameters.month.HasValue)
                Parameters.month = _clock.Now.Month;
            // TODO: Need a test-definable date!

            // User Story AB#1120: Summary report optimization: Run the report once, then extract pieces of the report out of the master
            //
            // The idea here is to build a single report doing all the SQL queries just ONCE, then carve it up into
            // the various reports we want to display

            var buildparms = new ReportParameters() { slug = "all-summary", month = Parameters.month, year = parms.year };
            var all = Build(buildparms);

            var result = new List<List<IDisplayReport>>();

            // Slice out the Income report
            var leftside = new List<IDisplayReport>();
            var incomereport = all.TakeSlice("Income");
            incomereport.SortOrder = Report.SortOrders.TotalAscending;
            incomereport.Definition = "income";
            leftside.Add(incomereport);

            // Slice out the Taxes report
            var taxesreport = all.TakeSlice("Taxes");
            taxesreport.Definition = "taxes";
            leftside.Add(taxesreport);

            // Create a manual Net Income report: Income + Taxes

            var netincomereport = new ManualReport() { Name = "Net Income", Description = "--" };
            var incomerow = new RowLabel() { Name = "Income" };
            var taxesrow = new RowLabel() { Name = "Taxes" };
            var pctcol = new ColumnLabel() { Name = "% of Income", IsSortingAfterTotal = true, DisplayAsPercent = true };
            var income = incomereport.GrandTotal;
            var taxes = taxesreport.GrandTotal;
            netincomereport.SetData(new[] 
            {
                (ColumnLabel.Total, incomerow,      income),
                (ColumnLabel.Total, taxesrow,       taxes),
                (ColumnLabel.Total, RowLabel.Total, income + taxes),
                (pctcol,            incomerow,      1m),
                (pctcol,            taxesrow,       (income == 0) ? 0 : -taxes / income),
                (pctcol,            RowLabel.Total, (income == 0) ? 0 : (income + taxes) / income)
            });
            leftside.Add(netincomereport);

            // Add the left side to the final result
            result.Add(leftside);

            // Now prepare the right side
            var rightside = new List<IDisplayReport>();

            // Add the expenses report
            var expensesreport = all.TakeSliceExcept(new string[] { "Savings", "Income", "Taxes", "Transfer", "Unmapped" });
            expensesreport.PruneToLevel(1);
            expensesreport.Definition = "expenses";
            expensesreport.Name = "Expenses";
            rightside.Add(expensesreport);

            // Slice out the savings report
            var savingsreport = all.TakeSlice("Savings");
            savingsreport.Definition = "savings";
            savingsreport.Name = "Explicit Savings";
            rightside.Add(savingsreport);

            // Create a new manual Profit report: Income + Taxes + Expenses
            var profitreport = new ManualReport() { Name = "Net Savings" };
            var netincomerow = new RowLabel() { Name = "Net Income" };
            var expensesrow = new RowLabel() { Name = "Expenses" };
            pctcol = new ColumnLabel() { Name = "% of Net Income", IsSortingAfterTotal = true, DisplayAsPercent = true };
            var netincome = netincomereport.GrandTotal;
            var expenses = expensesreport.GrandTotal;
            profitreport.SetData(new[]
            {
                (ColumnLabel.Total, netincomerow,   netincome),
                (ColumnLabel.Total, expensesrow,    expenses),
                (ColumnLabel.Total, RowLabel.Total, netincome + expenses),
                (pctcol,            netincomerow,   1m),
                (pctcol,            expensesrow,    (netincome == 0) ? 0 : -expenses / netincome),
                (pctcol,            RowLabel.Total, (netincome == 0) ? 0 : (netincome + expenses) / netincome)
            });
            rightside.Add(profitreport);

            // Add the right side to the final result
            result.Add(rightside);

            return result;
        }

#region IReportEngine
        IEnumerable<ReportDefinition> IReportEngine.Definitions => Definitions;
#endregion

        public static List<ReportDefinition> Definitions = new List<ReportDefinition>()
        {
            new ReportDefinition()
            {
                slug = "income",
                SkipLevels = 1,
                SortOrder = "TotalAscending",
                Name = "Income",
                Source = "Actual",
                SourceParameters = "top=Income",
                CustomColumns = "pctoftotal"
            },
            new ReportDefinition()
            {
                slug = "taxes",
                Source = "Actual",
                SourceParameters = "top=Taxes",
                SkipLevels = 1,
                Name = "Taxes",
                CustomColumns = "pctoftotal"
            },
            new ReportDefinition()
            {
                slug = "expenses",
                CustomColumns = "pctoftotal",
                Source = "Actual",
                SourceParameters = "excluded=Savings,Taxes,Income,Transfer,Unmapped",
                NumLevels = 2,
                Name = "Expenses",
            },
            new ReportDefinition()
            {
                slug = "savings",
                Source = "Actual",
                SourceParameters = "top=Savings",
                SkipLevels = 1,
                Name = "Savings",
                CustomColumns = "pctoftotal"
            },
            new ReportDefinition()
            {
                slug = "income-detail",
                SkipLevels = 1,
                SortOrder = "TotalAscending",
                Name = "Income Detail",
                Source = "Actual",
                SourceParameters = "top=Income",
                CustomColumns = "pctoftotal",
                NumLevels = 2,
                WithMonthColumns = true,
            },
            new ReportDefinition()
            {
                slug = "taxes-detail",
                Source = "Actual",
                SourceParameters = "top=Taxes",
                SkipLevels = 1,
                Name = "Taxes Detail",
                CustomColumns = "pctoftotal",
                NumLevels = 2,
                WithMonthColumns = true,

            },
            new ReportDefinition()
            {
                slug = "expenses-detail",
                CustomColumns = "pctoftotal",
                Source = "Actual",
                SourceParameters = "excluded=Savings,Taxes,Income,Transfer,Unmapped",
                Name = "Expenses Detail",
                NumLevels = 2,
                WithMonthColumns = true,
            },
            new ReportDefinition()
            {
                slug = "savings-detail",
                Source = "Actual",
                SourceParameters = "top=Savings",
                SkipLevels = 1,
                Name = "Savings Detail",
                CustomColumns = "pctoftotal",
                WithMonthColumns = true,
                NumLevels = 2,
            },
            new ReportDefinition()
            {
                slug = "all",
                NumLevels = 2,
                Source = "Actual",
                Name = "All Transactions",
                WithMonthColumns = true,
            },
            new ReportDefinition()
            {
                slug = "all-summary",
                NumLevels = 2,
                Source = "Actual",
                Name = "All Transactions Summary",
                CustomColumns = "pctoftotal"
            },
            new ReportDefinition()
            {
                slug = "trips",
                Source = "Actual",
                SourceParameters = "top=Travel:Trips",
                SkipLevels = 2,
                NumLevels = 2,
                Name = "Travel Trips",
                CustomColumns = "pctoftotal"
            },
            new ReportDefinition()
            {
                slug = "budget",
                Name = "Full Budget",
                Source = "Budget",
                WholeYear = true,
                NumLevels = 2,
            },
            new ReportDefinition()
            {
                slug = "all-v-budget",
                Name = "All vs. Budget",
                Source = "ActualVsBudget",
                WholeYear = true,
                WithTotalColumn = false,
                NumLevels = 2,
                CustomColumns = "budgetpct",
            },
            new ReportDefinition()
            {
                slug = "expenses-budget",
                Source = "Budget",
                SourceParameters = "excluded=Savings,Taxes,Income,Transfer,Unmapped",
                WholeYear = true,
                NumLevels = 2,
                Name = "Expenses Budget"
            },
            new ReportDefinition()
            {
                slug = "expenses-v-budget",
                Name = "Expenses vs. Budget",
                Source = "ActualVsBudget",
                SourceParameters = "excluded=Savings,Taxes,Income,Transfer,Unmapped",
                WholeYear = true,
                YearProgress = true,
                CustomColumns = "budgetpct,budgetavailable",
                WithTotalColumn = false,
                NumLevels = 2,
            },
            new ReportDefinition()
            {
                slug = "managed-budget",
                Name = "Managed Budget",
                Source = "ManagedBudget",
                WithTotalColumn = false,
                NumLevels = 4,
                DisplayLevelAdjustment = 1,
                SortOrder = "NameAscending",
                CustomColumns = "budgetpct,budgetavailable",
            },
            new ReportDefinition()
            {
                slug = "export",
                Name = "Transaction Export",
                Source = "ActualVsBudget",
                SourceParameters = "leafrows=true",
                WithTotalColumn = false,
                NumLevels = 4,
                SortOrder = "NameAscending",
            },
            new ReportDefinition()
            {
                slug = "yoy",
                Name = "Year over Year",
                Source = "YearOverYear",
                NumLevels = 2,
            }
        };
    }
}
