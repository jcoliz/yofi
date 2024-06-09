using System.Collections.Generic;
using System.Linq;

namespace YoFi.Core.Reports;

/// <summary>
/// Report suitable for transmission over the network
/// </summary>
/// <remarks>
/// TODO: This is going to need more work. Custom columns contain a code Func<>.
/// This is not suitable for transmission. So at a minimum, I need to calculate
/// calculated columns. Generally speaking, I can probably greatly reduce the
/// amount of info in reports and columns by pre-processing a bit more.
/// </remarks>
public class WireReport : IDisplayReport
{
    public string Name { get; set; }

    public string Description { get; set; }

    public string Definition { get; set; }

    public List<ColumnLabel> ColumnLabels { get; set; }

    public List<RowLabel> RowLabels { get; set; }

    public int DisplayLevelAdjustment { get; set; }

    public decimal GrandTotal { get; set; }

    /// <summary>
    /// One line per row, in same oreder as RowLabelsOrdered
    /// </summary>
    public List<WireReportLine> Lines { get; set; }

    IEnumerable<ColumnLabel> IDisplayReport.ColumnLabelsFiltered => ColumnLabels;

    IEnumerable<RowLabel> IDisplayReport.RowLabelsOrdered => RowLabels;

    decimal IDisplayReport.this[ColumnLabel column, RowLabel row] 
    {
        get
        {
            var rownum = RowLabels.IndexOf(row);
            var rowdata = Lines[rownum];
            var colnum = ColumnLabels.IndexOf(column);
            var result = rowdata.Values[colnum];
            return result;
        }
    }

    public static WireReport BuildFrom(IDisplayReport report)
    {
        var result = new WireReport();

        result.Name = report.Name;
        result.Description = report.Description;
        result.Definition = report.Definition;
        result.DisplayLevelAdjustment = report.DisplayLevelAdjustment;
        result.GrandTotal = report.GrandTotal;

        result.ColumnLabels = report.ColumnLabelsFiltered.ToList();
        result.RowLabels = report.RowLabelsOrdered.ToList();

        result.Lines = result.RowLabels
            .Select(row => 
                new WireReportLine() 
                { 
                    Name = row.ToString(),
                    Values = result.ColumnLabels.Select(col => report[col,row]).ToList()
                })
            .ToList();

        return result;
    }
}

public class WireReportLine
{
    /// <summary>
    /// Display name for this line
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// One value per column label, in same order as containing object's
    /// ColumnLabelsFiltered
    /// </summary>
    public List<decimal> Values { get; set; }
}