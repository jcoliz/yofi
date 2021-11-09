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
        public ReportBuilder(IDataContext context)
        {
            _qbuilder = new QueryBuilder(context);
        }

        private readonly QueryBuilder _qbuilder;

        /// <summary>
        /// Build the report from the given <paramref name="parameters"/>
        /// </summary>
        /// <param name="parameters">Parameters describing the report to be built</param>
        /// <returns>The report we built</returns>
        public Report Build(ReportParameters parameters)
        {
            var report = new Report();

            int Month = _qbuilder.Month = parameters.month ?? 12;
            int Year = _qbuilder.Year = parameters.year ?? DateTime.Now.Year;

            if (!Definitions.Any(x => x.id == parameters.id))
                throw new KeyNotFoundException($"Unable to find report {parameters.id}");

            var definition = Definitions.Where(x => x.id == parameters.id).SingleOrDefault();

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

            // WholeYear needs to be handled after source is set

            if (definition.YearProgress == true)
            {
                // What is the highest transaction in the "Actuals"?
                var latesttime = report.Source.Where(x => x.Name == "Actual").Select(q => q.Query.DefaultIfEmpty().Max(a => (a == null) ? DateTime.MinValue : a.Timestamp)).Max();

                // What % of the way is it through that year?
                var yearprogress = (double)latesttime.DayOfYear / 365.0;

                report.Description += $" {yearprogress:P0}";
            }

            // Special case for yoy report

            if (parameters.id == "yoy")
            {
                var years = report.Source.Select(x => Int32.Parse(x.Name));
                report.Description = $"For {years.Min()} to {years.Max()}";
            }

            // Set custom columns

            if (!string.IsNullOrEmpty(definition.CustomColumns))
                foreach (var col in definition.CustomColumns.Split(","))
                    report.AddCustomColumn(CustomColumnFor(col,report));

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

        private ColumnLabel CustomColumnFor(string name,Report report)
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
                        (report.GrandTotal == 0) ? 0 : cols.GetValueOrDefault("TOTAL") / report.GrandTotal
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

        #region IReportEngine
        IEnumerable<ReportDefinition> IReportEngine.Definitions => Definitions;
        #endregion

        public static List<ReportDefinition> Definitions = new List<ReportDefinition>()
        {
            new ReportDefinition()
            {
                id = "income",
                SkipLevels = 1,
                SortOrder = "TotalAscending",
                Name = "Income",
                Source = "Actual",
                SourceParameters = "top=Income",
                CustomColumns = "pctoftotal"
            },
            new ReportDefinition()
            {
                id = "taxes",
                Source = "Actual",
                SourceParameters = "top=Taxes",
                SkipLevels = 1,
                Name = "Taxes",
                CustomColumns = "pctoftotal"
            },
            new ReportDefinition()
            {
                id = "expenses",
                CustomColumns = "pctoftotal",
                Source = "Actual",
                SourceParameters = "excluded=Savings,Taxes,Income,Transfer,Unmapped",
                Name = "Expenses",
            },
            new ReportDefinition()
            {
                id = "savings",
                Source = "Actual",
                SourceParameters = "top=Savings",
                SkipLevels = 1,
                Name = "Savings",
                CustomColumns = "pctoftotal"
            },
            new ReportDefinition()
            {
                id = "income-detail",
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
                id = "taxes-detail",
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
                id = "expenses-detail",
                CustomColumns = "pctoftotal",
                Source = "Actual",
                SourceParameters = "excluded=Savings,Taxes,Income,Transfer,Unmapped",
                Name = "Expenses Detail",
                NumLevels = 2,
                WithMonthColumns = true,
            },
            new ReportDefinition()
            {
                id = "savings-detail",
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
                id = "all",
                NumLevels = 2,
                Source = "Actual",
                Name = "All Transactions",
                WithMonthColumns = true,
            },
            new ReportDefinition()
            {
                id = "trips",
                Source = "Actual",
                SourceParameters = "top=Travel:Trips",
                SkipLevels = 2,
                NumLevels = 2,
                Name = "Travel Trips",
                CustomColumns = "pctoftotal"
            },
            new ReportDefinition()
            {
                id = "budget",
                Name = "Full Budget",
                Source = "Budget",
                WholeYear = true,
                NumLevels = 2,
            },
            new ReportDefinition()
            {
                id = "all-v-budget",
                Name = "All vs. Budget",
                Source = "ActualVsBudget",
                WholeYear = true,
                WithTotalColumn = false,
                NumLevels = 2,
                CustomColumns = "budgetpct",
            },
            new ReportDefinition()
            {
                id = "expenses-budget",
                Source = "Budget",
                SourceParameters = "excluded=Savings,Taxes,Income,Transfer,Unmapped",
                WholeYear = true,
                NumLevels = 2,
                Name = "Expenses Budget"
            },
            new ReportDefinition()
            {
                id = "expenses-v-budget",
                Name = "Expenses vs. Budget",
                Source = "ActualVsBudget",
                SourceParameters = "excluded=Savings,Taxes,Income,Transfer,Unmapped",
                WholeYear = true,
                YearProgress = true,
                CustomColumns = "budgetpct",
                WithTotalColumn = false,
                NumLevels = 2,
            },
            new ReportDefinition()
            {
                id = "managed-budget",
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
                id = "export",
                Name = "Transaction Export",
                Source = "ActualVsBudget",
                SourceParameters = "leafrows=true",
                WithTotalColumn = false,
                NumLevels = 4,
                SortOrder = "NameAscending",
            },
            new ReportDefinition()
            {
                id = "yoy",
                Name = "Year over Year",
                Source = "YearOverYear",
                NumLevels = 2,
            }
        };
    }
}
