using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.AspNet.Controllers.Reports
{
    /// <summary>
    /// A report you create yourself by filling in the table
    /// </summary>
    /// <remarks>
    /// Because it implements IDisplayReport, it can be displayed the same was as a
    /// fully-created report
    /// </remarks>
    public class ManualReport : Table<ColumnLabel, RowLabel, decimal>, IDisplayReport
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public IEnumerable<ColumnLabel> ColumnLabelsFiltered => base.ColumnLabels;

        public IEnumerable<RowLabel> RowLabelsOrdered => base.RowLabels;

        public int DisplayLevelAdjustment { get; set; }

        /// <summary>
        /// The row which contains the total for columns
        /// </summary>
        public RowLabel TotalRow { get; } = new RowLabel() { IsTotal = true };

        /// <summary>
        /// The column which contains the total for rows
        /// </summary>
        public ColumnLabel TotalColumn { get; } = new ColumnLabel() { IsTotal = true };

        /// <summary>
        /// The total of the total column
        /// </summary>
        public decimal GrandTotal => this[TotalColumn, TotalRow];
    }
}
