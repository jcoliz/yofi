using System;
using System.Collections.Generic;

namespace YoFi.Core.Reports;

/// <summary>
/// A report you create yourself by filling in the table
/// </summary>
/// <remarks>
/// Because it implements IDisplayReport, it can be displayed the same was as a
/// fully-created report
/// </remarks>
public class ManualReport : Table<ColumnLabel, RowLabel, decimal>, IDisplayReport
{
    /// <summary>
    /// Display name of the report
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// In-depth description of the report for display
    /// </summary>
    /// <remarks>
    /// In practice, this is used exclusively to describe the timeframe covered by
    /// the report, so could be renamed "timeframe"
    /// </remarks>
    public string Description { get; set; }

    /// <summary>
    /// All the column labels which should be displayed
    /// </summary>
    public IEnumerable<ColumnLabel> ColumnLabelsFiltered => base.ColumnLabels;

    /// <summary>
    /// All the row labels, in the order to display
    /// </summary>
    public IEnumerable<RowLabel> RowLabelsOrdered => base.RowLabels;

    /// <summary>
    /// When displaying the report, how many levels higher to show as
    /// </summary>
    /// <remarks>
    /// Typically the lowest-level of display formatting doesn't look great on its
    /// own, so when NumLevels is 1, it's good to set DisplayLevelAdjustment to 1
    /// and make it look like a heading.
    /// </remarks>
    public int DisplayLevelAdjustment { get; set; }

    /// <summary>
    /// The row which contains the total for columns
    /// </summary>
    public static RowLabel TotalRow => RowLabel.Total;

    /// <summary>
    /// The column which contains the total for rows
    /// </summary>
    public static ColumnLabel TotalColumn => ColumnLabel.Total;

    /// <summary>
    /// The total of the total column
    /// </summary>
    public decimal GrandTotal => this[TotalColumn, TotalRow];

    /// <summary>
    /// Report definition
    /// </summary>
    /// <remarks>
    /// Because this is a manual report, no definition was used in its making.
    /// However, sometimes we use this as a report tag for testing.
    /// </remarks>
    public string Definition { get; set; }

    /// <summary>
    /// Bulk set datapoints
    /// </summary>
    /// <param name="datapoints">Array of (col,row,value) tuples</param>
    internal void SetData((ColumnLabel col, RowLabel row, decimal value)[] datapoints)
    {
        foreach (var (col, row, value) in datapoints)
            this[col, row] = value;
    }
}
