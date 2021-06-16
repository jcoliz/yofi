using OfxWeb.Asp.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Controllers.Helpers
{
    public class Report : Table<ColumnLabel, RowLabel, decimal>, IComparer<BaseLabel>
    {

        public bool WithMonthColumns { get; set; } = false;

        public string Name { get; set; } = "Report";

        public string Description { get; set; }

        public int FromLevel { get; set; }

        public int NumLevels { get; set; } = 1;

        public int DisplayLevelAdjustment { get; set; }

        public IQueryable<IReportable> SingleSource { get; set; }

        public IEnumerable<IGrouping<string, IReportable>> SeriesSource { get; set; }

        public RowLabel TotalRow { get; }  = new RowLabel() { IsTotal = true };
        public ColumnLabel TotalColumn { get; } = new ColumnLabel() { IsTotal = true };

        public IEnumerable<RowLabel> RowLabelsOrdered => base.RowLabels.OrderBy(x => x, this);

        /// <summary>
        /// This will build a one-level report with no columns, just totals,
        /// headings as first-level categories, rows as second-level categories
        /// </summary>
        /// <remarks>
        /// e.g. Transactions where category is X:A:C or X:A:B will all be lumped on X:A.
        /// </remarks>
        /// <param name="items"></param>

        public void Build()
        {
            if (NumLevels < 1)
                throw new ArgumentOutOfRangeException(nameof(NumLevels), "Must be 1 or greater");

            if (SingleSource != null)
            {
                BuildInternal(SingleSource, FromLevel, NumLevels, null);
            }

            if (SeriesSource != null)
            {
                foreach (var series in SeriesSource)
                    BuildInternal(series.AsQueryable(), FromLevel, NumLevels, null, new ColumnLabel() { Name = series.Key });
            }

            CalculateTotalRow(NumLevels - 1);
        }

        void BuildInternal(IQueryable<IReportable> items, int fromlevel, int numlevels, RowLabel parent, ColumnLabel seriescolumn = null)
        {
            var groups = items.GroupBy(x => GetTokenByIndex(x.Category, fromlevel));

            if (groups.Count() > 1 || groups.Single().Key != null) // Skip empty sub-levels
            {
                foreach (var group in groups)
                {
                    var token = group.Key;
                    var newpath = parent == null ? token : $"{parent.UniqueID}:{token}";
                    var row = new RowLabel() { Name = token, Level = numlevels - 1, UniqueID = newpath, Parent = parent };

                    var sum = group.Sum(x => x.Amount);
                    base[TotalColumn, row] += sum;
                    if (seriescolumn != null)
                        base[seriescolumn, row] += sum;

                    if (WithMonthColumns)
                        foreach (var monthgroup in group.GroupBy(x => x.Timestamp.Month))
                        {
                            var month = monthgroup.Key;
                            var column = new ColumnLabel() { UniqueID = month.ToString("D2"), Name = new DateTime(2000, month, 1).ToString("MMM") };
                            if (seriescolumn != null)
                            {
                                column.UniqueID += ":" + seriescolumn.Name;
                                column.Name += " " + seriescolumn.Name;
                            }
                            base[column, row] = monthgroup.Sum(x => x.Amount);
                        }

                    // Build next level down
                    if (numlevels > 1 && token != null)
                        BuildInternal(group.AsQueryable(), fromlevel + 1, numlevels - 1, row, seriescolumn);
                }
            }
        }

        void CalculateTotalRow(int usinglevel)
        {
            // Calculate column totals from top-level headings
            foreach (var row in base.RowLabels.Where(x => x.Level == usinglevel))
                foreach (var col in base.ColumnLabels)
                    base[col, TotalRow] += base[col, row];
        }

        public void WriteToConsole(bool sorted = false)
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

            var rows = sorted ? RowLabelsOrdered : RowLabels;
            foreach (var line in rows)
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

        int IComparer<BaseLabel>.Compare(BaseLabel x, BaseLabel y)
        {
            int result = 0;
            var n = new RowLabel() { Name = "null" };

            Console.WriteLine($"Compare [{x??n}] vs [{y??n}]");

            // (1) Total rows always come after other rows
            if (x.IsTotal && y.IsTotal)
                goto done;
            if (x.IsTotal)
            {
                result = 1;
                goto done;
            }
            if (y.IsTotal)
            {
                result = -1;
                goto done;
            }

            // (2) If these two share a common parent, we can compare based on totals
            if (x.Parent == y.Parent)
            {
                var yval = base[TotalColumn, y as RowLabel];
                var xval = base[TotalColumn, x as RowLabel];
                Console.WriteLine($"Checking Totals: {xval:C2} vs {yval:C2}...");
                result = base[TotalColumn, y as RowLabel].CompareTo(base[TotalColumn, x as RowLabel]);
                goto done;
            }

            // (3) Parents always come before their own children

            if (x.Parent == y)
            {
                Console.WriteLine("Parent relationship");
                result = 1;
                goto done;
            }
            if (y.Parent == x)
            {
                Console.WriteLine("Parent relationship");
                result = -1;
                goto done;
            }

            // (4) we can also check grandparents
            // TODO

            // We need to start searching upward
            if (x.Parent != null)
            {
                Console.WriteLine($"Try {x} Parent vs {y}");
                result = ((IComparer<BaseLabel>)this).Compare(x.Parent, y);
                if (result != 0)
                    goto done;
            }
            if (y.Parent != null)
            {
                Console.WriteLine($"Try {x} vs {y} Parent");
                result = ((IComparer<BaseLabel>)this).Compare(x, y.Parent);
                if (result != 0)
                    goto done;
            }

            /*
             * This is broken. In this case, we needed to have checked Other:Something vs Other.Else
             * and done a totals check at that point. In this case it got the answer right because
             * Other:Something DOES have the higher total, but the logic is wrong.
             * 
                Compare [ID:Other:Something] vs [ID:Other:Else:]
                Try ID:Other:Something Parent vs ID:Other:Else:
                Compare [ID:Other] vs [ID:Other:Else:]
                Try ID:Other vs ID:Other:Else: Parent
                Compare [ID:Other] vs [ID:Other:Else]
                Parent relationship
                >> ID:Other is before ID:Other:Else
                >> ID:Other is before ID:Other:Else:
                >> ID:Other:Something is before ID:Other:Else:
             */


        // Otherwise we have no idea
#if false
            if (x == null && y == null)
                goto done;

            if (x == null)
            {
                result = -1;
                goto done;
            }

            if (y == null)
            {
                result = 1;
                goto done;
            }


            Console.WriteLine("Checking parents...");
            result = ((IComparer<BaseLabel>)this).Compare(x.Parent, y.Parent);

            if (result == 0)
            {
                // The only way to compare two rows at different levels of the hierarchy is to
                // compare children of a common parent.

                var yval = base[TotalColumn, y as RowLabel];
                var xval = base[TotalColumn, x as RowLabel];
                Console.WriteLine($"Checking Totals: {xval:C2} vs {yval:C2}...");
                result = base[TotalColumn, y as RowLabel].CompareTo(base[TotalColumn, x as RowLabel]);
            }
#endif
        done:

            if (result < 0)
                Console.WriteLine($">> {x??n} is before {y??n}");
            else if (result > 0)
                Console.WriteLine($">> {x??n} is after {y??n}");
            else
                Console.WriteLine($">> {x??n} is same as {y??n}");

            return result;
        }
    }

    public class BaseLabel: IComparable<BaseLabel>
    {
        /// <summary>
        /// Tag to uniquely identify the column
        /// </summary>
        /// <remarks>
        /// Currently used to sort, but that will change.
        /// </remarks>
        public string UniqueID { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the label
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Final total-displaying column
        /// </summary>
        public bool IsTotal { get; set; }

        /// <summary>
        /// In a multi-level report, whom are we under? or null for top-level
        /// </summary>
        public BaseLabel Parent { get; set; }

        public override bool Equals(object obj)
        {
            return obj is BaseLabel label &&
                   UniqueID == label.UniqueID &&
                   Name == label.Name &&
                   IsTotal == label.IsTotal;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UniqueID, Name, IsTotal);
        }

        public override string ToString()
        {
            if (IsTotal)
                return "TOTAL";
            if (!string.IsNullOrEmpty(UniqueID))
                return $"ID:{UniqueID}";
            else
                return $"Name:{Name}";
        }

        int IComparable<BaseLabel>.CompareTo(BaseLabel other)
        {
            int result = IsTotal.CompareTo(other.IsTotal);
            if (result == 0) // Empty orders sort at the END
                result = string.IsNullOrEmpty(UniqueID).CompareTo(string.IsNullOrEmpty(other.UniqueID));
            if (result == 0)
                result = UniqueID.CompareTo(other.UniqueID);
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
