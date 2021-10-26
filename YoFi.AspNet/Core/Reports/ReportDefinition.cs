using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.AspNet.Controllers.Reports
{
    /// <summary>
    /// Defines a single kind of report
    /// </summary>
    /// <remarks>
    /// The idea is to remove report defintion from code, into data. This object
    /// should be something we could later store in the database
    /// </remarks>
    public class ReportDefinition
    {
        /// <summary>
        /// The short name identifier
        /// </summary>
        public string id { get; set; }

        #region Interpreted by QueryBuilder

        /// <summary>
        /// The name of a query to be used as a source
        /// </summary>
        /// <remarks>
        /// This name must be known to <see cref="QueryBuilder"/>
        /// </remarks>
        public string Source { get; set; }

        /// <summary>
        /// Parameter to be used when generating the source query
        /// </summary>
        /// <remarks>
        /// In the form of "paramter:value,value,value"
        /// This name must be known to <see cref="QueryBuilder"/>
        /// </remarks>
        public string SourceParameters { get; set; }

        #endregion

        #region Interpreted by ReportBuilder

        /// <summary>
        /// Custom columns to include
        /// </summary>
        /// <remarks>
        /// Comma-separated list of columns, which must be known to ReportBuilder
        /// </remarks>
        public string CustomColumns { get; set; }

        /// <summary>
        /// Cover the whole year, despite current months parameter
        /// </summary>
        public bool? WholeYear { get; set; }

        /// <summary>
        /// Whether to display the precise year progress in description
        /// </summary>
        public bool? YearProgress { get; set; }

        #endregion

        #region Direct Members of Report

        /// <summary>
        /// The long readable name of the report
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Order to sort in, or null for default
        /// </summary>
        /// <remarks>
        /// Must be one of <see cref="Report.SortOrders"/>, or empty to use default
        /// </remarks>
        public string SortOrder { get; set; }

        /// <summary>
        /// Whether to include columns for individual months, or null to use default
        /// </summary>
        public bool? WithMonthColumns { get; set; }

        /// <summary>
        /// Whether to include a total column, or null to use default
        /// </summary>
        public bool? WithTotalColumn { get; set; }

        /// <summary>
        /// How many levels of depth to skip before showing the report, or null to use default
        /// So 0 means start at the top level,
        /// 1 is the 2nd level down
        /// </summary>
        public int? SkipLevels { get; set; }

        /// <summary>
        /// How many levels deep to include, or null to use default
        /// </summary>
        public int? NumLevels { get; set; }

        /// <summary>
        /// When displaying the report, how many levels higher to show as, or null to use default
        /// </summary>
        /// <remarks>
        /// Typically the lowest-level of display formatting doesn't look great on its
        /// own, so when NumLevels is 1, it's good to set DisplayLevelAdjustment to 1
        /// and make it look like a heading.
        /// </remarks>
        public int? DisplayLevelAdjustment { get; set; }

        #endregion
    }
}
