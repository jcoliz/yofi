using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Controllers.Helpers
{
    public class Report : Table<ColumnLabel, RowLabel, decimal>
    {
        public bool WithMonthColumns { get; set; } = false;

        public RowLabel TotalRow { get; }  = new RowLabel() { IsTotal = true };
        public ColumnLabel TotalColumn { get; } = new ColumnLabel() { IsTotal = true };

        /// <summary>
        /// This will build a one-level report with no columns, just totals,
        /// headings as first-level categories, rows as second-level categories
        /// </summary>
        /// <remarks>
        /// e.g. Transactions where category is X:A:C or X:A:B will all be lumped on X:A.
        /// </remarks>
        /// <param name="items"></param>

        public void Build(IQueryable<IReportable> items, int fromlevel, int tolevel)
        {
            BuildInternal(items, fromlevel, tolevel, null);

            // Calculate column totals from top-level headings
            foreach (var row in base.RowLabels.Where(x => x.Level == tolevel - fromlevel))
                foreach (var col in base.ColumnLabels)
                    base[col, TotalRow] += base[col, row];
        }

        void BuildInternal(IQueryable<IReportable> items, int fromlevel, int tolevel, string categorypath)
        {
            foreach (var group in items.GroupBy(x => GetTokenByIndex(x.Category, fromlevel)))
            {
                var token = group.Key;
                var newpath = string.IsNullOrEmpty(categorypath) ? token : $"{categorypath}:{token}";
                var row = new RowLabel() { Name = token, Level = tolevel - fromlevel, Order = newpath };
                base[TotalColumn, row] = group.Sum(x => x.Amount);

                if (WithMonthColumns)
                    foreach (var monthgroup in group.GroupBy(x => x.Timestamp.Month))
                    {
                        var month = monthgroup.Key;
                        var column = new ColumnLabel() { Order = month.ToString("D2"), Name = new DateTime(2000, month, 1).ToString("MMM") };
                        base[column, row] = monthgroup.Sum(x => x.Amount);
                    }

                // Build next level down
                if (fromlevel < tolevel && token != null)
                    BuildInternal(group.AsQueryable(), fromlevel + 1, tolevel, newpath);
            }
        }

        public void WriteToConsole()
        {
            var maxlevel = RowLabels.Max(x => x.Level);

            // Columns

            var builder = new StringBuilder();
            var name = string.Empty;
            var padding = String.Concat(Enumerable.Repeat<char>(' ', maxlevel));
            builder.Append($"+ {name,15}{padding} ");

            foreach (var col in ColumnLabels)
            {
                name = col.Name;
                if (col.IsTotal)
                    name = "TOTAL";
                builder.Append($"| {name,10} ");
            }

            Console.WriteLine(builder.ToString());

            // Rows

            foreach (var line in RowLabels)
            {
                builder = new StringBuilder();

                name = line.Name;
                if (line.IsTotal)
                    name = "TOTAL";
                if (name == null)
                    name = "-";

                var padding_before = String.Concat( Enumerable.Repeat<char>('>', line.IsTotal ? 0 : maxlevel - line.Level));
                var padding_after = String.Concat( Enumerable.Repeat<char>(' ', line.IsTotal ? maxlevel : line.Level));

                builder.Append($"{line.Level} {padding_before}{name,-15}{padding_after} ");

                foreach (var col in ColumnLabels)
                {
                    var val = this[col, line];
                    builder.Append($"| {val,10:C2} ");
                }

                Console.WriteLine(builder.ToString());
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
