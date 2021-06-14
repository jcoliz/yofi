using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Controllers.Helpers
{
    public class Report : PivotTable<ColumnLabel, RowLabel, decimal>
    {
        /// <summary>
        /// This will build a one-level report with columns as months,
        /// and rows as first-level categories
        /// </summary>
        /// <remarks>
        /// e.g. Transactions where category is X:Y or X:A:B will all be lumped on X.
        /// </remarks>
        /// <param name="items"></param>

        public void Build(IQueryable<IReportable> items, bool nocols = false)
        {
            var totalrow = new RowLabel() { IsTotal = true };
            var totalcolumn = new ColumnLabel() { IsTotal = true };

            // One row per top-level category
            var categorygroups = items.GroupBy(x => GetTokenByIndex(x.Category, 0));
            foreach (var categorygroup in categorygroups)
            {
                var category = categorygroup.Key;
                var row = new RowLabel() { Name = category };

                var sum = categorygroup.Sum(x => x.Amount);

                base[totalcolumn, row] += sum;
                base[totalcolumn, totalrow] += sum;

                // One column per month
                if (!nocols)
                {
                    var monthgroups = categorygroup.GroupBy(x => x.Timestamp.Month);
                    foreach (var monthgroup in monthgroups)
                    {
                        var month = monthgroup.Key;
                        var column = new ColumnLabel() { Order = month.ToString("D2"), Name = new DateTime(2000, month, 1).ToString("MMM") };

                        sum = monthgroup.Sum(x => x.Amount);

                        base[column, row] = sum;
                        base[column, totalrow] += sum;
                    }
                }
            }
        }

        /// <summary>
        /// This will build a one-level report with no columns, just totals,
        /// and rows as first-level categories
        /// </summary>
        /// <remarks>
        /// e.g. Transactions where category is X:Y or X:A:B will all be lumped on X.
        /// </remarks>
        /// <param name="items"></param>

        public void BuildNoCols(IQueryable<IReportable> items) => Build(items, true);


        /// <summary>
        /// This will build a one-level report with no columns, just totals,
        /// headings as first-level categories, rows as second-level categories
        /// </summary>
        /// <remarks>
        /// e.g. Transactions where category is X:A:C or X:A:B will all be lumped on X:A.
        /// </remarks>
        /// <param name="items"></param>

        public void BuildTwoLevel(IQueryable<IReportable> items, bool nocols = true)
        {
            var totalrow = new RowLabel() { IsTotal = true };
            var totalcolumn = new ColumnLabel() { IsTotal = true };

            // One heading per top-level category
            var categorygroups = items.GroupBy(x => GetTokenByIndex(x.Category, 0));
            foreach (var categorygroup in categorygroups)
            {
                var category = categorygroup.Key;
                var headingrow = new RowLabel() { Name = category, Level = 1, Order = category };

                var sum = categorygroup.Sum(x => x.Amount);
                base[totalcolumn, headingrow] = sum;
                base[totalcolumn, totalrow] += sum;

                // One row per second -level category
                var subcategorygroups = categorygroup.GroupBy(x => GetTokenByIndex(x.Category, 1));
                foreach (var subcategorygroup in subcategorygroups)
                {
                    var subcategory = subcategorygroup.Key ?? "-";
                    var row = new RowLabel() { Name = subcategory, Order = $"{category}:{subcategory}" };

                    sum = subcategorygroup.Sum(x => x.Amount);

                    base[totalcolumn, row] += sum;

                    // One column per month
                    if (!nocols)
                    {
                        var monthgroups = subcategorygroup.GroupBy(x => x.Timestamp.Month);
                        foreach (var monthgroup in monthgroups)
                        {
                            var month = monthgroup.Key;
                            var column = new ColumnLabel() { Order = month.ToString("D2"), Name = new DateTime(2000, month, 1).ToString("MMM") };

                            sum = monthgroup.Sum(x => x.Amount);

                            base[column, row] = sum;
                            base[column, totalrow] += sum;
                        }
                    }
                }
            }
        }

        static string GetTokenByIndex(string category, int index)
        {
            var split = category.Split(':');

            if (index >= split.Count() || split[index] == string.Empty)
                return null;
            else
                return split[index];
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
