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
            var totalrow = new RowLabel() { IsTotal = true };
            var totalcolumn = new ColumnLabel() { IsTotal = true };

            // One column per month
            var monthgroups = items.GroupBy(x => x.Timestamp.Month);
            foreach (var monthgroup in monthgroups)
            {
                var month = monthgroup.Key;
                var column = new ColumnLabel() { Order = month.ToString("D2"), Name = new DateTime(2000, month, 1).ToString("MMM") };

                // One row per top-level category
                var categorygroups = monthgroup.GroupBy(x => SplitCategory(x.Category, 1)[0]);
                foreach (var categorygroup in categorygroups)
                {
                    var category = categorygroup.Key;
                    var row = new RowLabel() { Name = category };

                    var sum = categorygroup.Sum(x => x.Amount);

                    base[column, row] = sum;
                    base[totalcolumn, row] += sum;
                    base[column, totalrow] += sum;
                }
            }
        }

        static List<string> SplitCategory(string category, int minitems)
        {
            var result = new List<String>();
            result.AddRange(category.Split(':'));

            if (result.Count < minitems)
                result.AddRange(Enumerable.Repeat<string>(null, minitems - result.Count));

            return result;
        }
    }

    public class BaseLabel: IComparable<BaseLabel>
    {
        /// <summary>
        /// Display order. Lower values display before higher values
        /// </summary>
        public string Order { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the label
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Final total-displaying column
        /// </summary>
        public bool IsTotal { get; set; }

        public override bool Equals(object obj)
        {
            return obj is BaseLabel label &&
                   Order == label.Order &&
                   Name == label.Name &&
                   IsTotal == label.IsTotal;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Order, Name, IsTotal);
        }

        int IComparable<BaseLabel>.CompareTo(BaseLabel other)
        {
            int result = IsTotal.CompareTo(other.IsTotal);
            if (result == 0)
                result = Order.CompareTo(other.Order);
            if (result == 0)
                result = Name.CompareTo(other.Name);

            return result;
        }
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
