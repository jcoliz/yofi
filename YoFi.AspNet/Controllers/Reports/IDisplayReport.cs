using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.AspNet.Controllers.Reports
{
    /// <summary>
    /// The interface used by the report renderer to display a report in HTML
    /// </summary>
    /// <remarks>
    /// Implement this interface and your class can look just like a real report!
    /// </remarks>
    public interface IDisplayReport
    {
        /// <summary>
        /// Display name of the report
        /// </summary>
        string Name { get; }

        /// <summary>
        /// In-depth description of the report for display
        /// </summary>
        /// <remarks>
        /// In practice, this is used exclusively to describe the timeframe covered by
        /// the report, so could be renamed "timeframe"
        /// </remarks>
        string Description { get; }

        /// <summary>
        /// All the column labels which should be displayed
        /// </summary>
        IEnumerable<ColumnLabel> ColumnLabelsFiltered { get; }

        /// <summary>
        /// All the row labels, in the order to display
        /// </summary>
        IEnumerable<RowLabel> RowLabelsOrdered { get; }

        /// <summary>
        /// When displaying the report, how many levels higher to show as
        /// </summary>
        /// <remarks>
        /// Typically the lowest-level of display formatting doesn't look great on its
        /// own, so when NumLevels is 1, it's good to set DisplayLevelAdjustment to 1
        /// and make it look like a heading.
        /// </remarks>
        int DisplayLevelAdjustment { get; }

        /// <summary>
        /// Indexer to retrieve report cells
        /// </summary>
        /// <param name="column">Which column</param>
        /// <param name="row">Which row</param>
        /// <returns>Value at that <paramref name="column"/>,<paramref name="row"/> position </returns>
        decimal this[ColumnLabel column, RowLabel row] { get; }

        /// <summary>
        /// The total of the total column
        /// </summary>
        public decimal GrandTotal { get; }

    }
}
