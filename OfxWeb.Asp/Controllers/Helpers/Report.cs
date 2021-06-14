using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Controllers.Helpers
{
    public class Report: PivotTable<ColumnLabel,RowLabel,decimal>
    {
        /// <summary>
        /// This will build a one-level report with columns as months,
        /// and rows as first-level categories
        /// </summary>
        /// <remarks>
        /// e.g. Transactions where category is X:Y or X:A:B will all be lumped on X.
        /// </remarks>
        /// <param name="items"></param>

        public void Build(IQueryable<IReportable> items)
        {
        }
    }

    public class BaseLabel
    {
        /// <summary>
        /// Display order. Lower values display before higher values
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Display name of the label
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Final total-displaying column
        /// </summary>
        public bool IsTotal { get; set; }
    }

    public class RowLabel: BaseLabel
    {
        /// <summary>
        /// How many levels ABOVE regular data is this?
        /// </summary>
        /// <remarks>
        /// Regular, lowest data is Level 0. First heading above that is Level 1,
        /// next heading up is level 2, etc.
        /// </remarks>
        public int Level { get; set; }
    }

    public class ColumnLabel : BaseLabel
    { 
    }

}
