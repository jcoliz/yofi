using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace OfxWeb.Asp.Controllers.Helpers
{
    /// <summary>
    /// Table-based report: Arranges IReportable items into tables
    /// </summary>
    /// <remarks>
    /// General usage: 
    /// 1. Set the Source
    /// 2. Set any needed configuration properties
    /// 3. Build the report
    /// 4. Iterate over rows/columns to display the report
    /// </remarks>
    public class Report : Table<ColumnLabel, RowLabel, decimal>, IComparer<RowLabel>
    {
        #region Configuration Properties

        /// <summary>
        /// Display name of the report
        /// </summary>
        public string Name { get; set; } = "Report";

        /// <summary>
        /// In-depth description of the report for display
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Single sources of items
        /// </summary>
        /// <remarks>
        /// Must set at least one source of data
        /// </remarks>
        public IQueryable<IReportable> SingleSource { get; set; }

        public Dictionary<string,IQueryable<IReportable>> MultipleSources { get; set; }

        /// <summary>
        /// Multiple sources of items
        /// </summary>
        /// <remarks>
        /// Must set at least one source of data
        /// </remarks>
        private IEnumerable<IGrouping<string, IReportable>> SeriesSource { get; set; }

        /// <summary>
        /// Multiple sources of items (IQueryable)
        /// </summary>
        /// <remarks>
        /// Must set at least one source of data
        /// </remarks>
        private IEnumerable<IQueryable<IGrouping<string, IReportable>>> SeriesQuerySource { get; set; }

        /// <summary>
        /// Whether to include columns for individual months
        /// </summary>
        public bool WithMonthColumns { get; set; } = false;

        /// <summary>
        /// Whether to include a total column
        /// </summary>
        public bool WithTotalColumn { get; set; } = true;

        /// <summary>
        /// How many levels of depth to skip before showing the report
        /// So 0 means start at the top level,
        /// 1 is the 2nd level down
        /// </summary>
        public int SkipLevels { get; set; }

        /// <summary>
        /// How many levels deep to include
        /// </summary>
        public int NumLevels { get; set; } = 1;

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
        /// All the possible ways you can sort the report
        /// </summary>
        public enum SortOrders { NameAscending, TotalAscending, TotalDescending };

        /// <summary>
        /// In what order should the report be sorted
        /// </summary>
        public SortOrders SortOrder { get; set; } = SortOrders.TotalDescending;

        /// <summary>
        /// Which column to use for ordering when SortOrder is set to TotalAscending or 
        /// TotalDescending. 
        /// </summary>
        /// <remarks>
        /// By default TotalColumn is used, unless it isn't being shown, in which case the
        /// first column is used
        /// </remarks>
        public ColumnLabel OrderingColumn
        {
            get
            {
                if (_OrderingColumn != null)
                    return _OrderingColumn;
                if (WithTotalColumn)
                    return TotalColumn;

                return ColumnLabels.First();
            }
            set
            {
                _OrderingColumn = value;
            }
        }
        ColumnLabel _OrderingColumn;

        /// <summary>
        /// We are only interested in seeing a flat representation of only the leaf rows
        /// </summary>
        /// <remarks>
        /// This is useful for reports where you only want the leaf items
        /// </remarks>
        public bool LeafRowsOnly { get; set; } = false;

        #endregion

        #region Informative Properties

        /// <summary>
        /// Indexer to retrieve report cells
        /// </summary>
        /// <param name="collabel">Which column</param>
        /// <param name="rowlabel">Which row</param>
        /// <returns>Value at that column,row position </returns>
        public new decimal this[ColumnLabel collabel, RowLabel rowlabel]
        {
            get
            {
                if (collabel.IsCalculated)
                    return collabel.Custom(RowDetails(rowlabel));
                else
                    return base[collabel, rowlabel];
            }
        }

        /// <summary>
        /// All the row labels, in the correct order as specified by SortOrder
        /// </summary>
        public IEnumerable<RowLabel> RowLabelsOrdered => base.RowLabels.OrderBy(x => x, this);

        /// <summary>
        /// All the column labels which should be shown, as modified by configuration properties
        /// </summary>
        public IEnumerable<ColumnLabel> ColumnLabelsFiltered => base.ColumnLabels.Where(x => WithTotalColumn || !x.IsTotal);

        /// <summary>
        /// The row which contains the total for columns
        /// </summary>
        public RowLabel TotalRow { get; } = new RowLabel() { IsTotal = true };

        /// <summary>
        /// The column which contains the total for rows
        /// </summary>
        public ColumnLabel TotalColumn { get; } = new ColumnLabel() { IsTotal = true };

        #endregion

        #region Public Methods

        /// <summary>
        /// Add a custom calculated column 
        /// </summary>
        /// <param name="column"></param>
        public void AddCustomColumn(ColumnLabel column)
        {
            if (column.Custom == null)
                throw new ArgumentOutOfRangeException(nameof(column), "Column must contain a custom function");

            column.IsCalculated = true;
            column.IsTotal = false;
            base._ColumnLabels.Add(column);
        }

        /// <summary>
        /// Build the report
        /// </summary>
        public void Build()
        {
            if (NumLevels < 1)
                throw new ArgumentOutOfRangeException(nameof(NumLevels), "Must be 1 or greater");

            bool V3 = false;
            if (SingleSource != null && SingleSource.Any())
            {
                V3 = true;
                BuildSingleV3(SingleSource);
            }
            if (MultipleSources != null)
            {
                V3 = true;
                foreach (var kvp in MultipleSources)
                {
                    var seriescolumn = new ColumnLabel() { Name = kvp.Key, UniqueID = kvp.Key };
                    BuildSingleV3(kvp.Value,seriescolumn);
                }
            }

            if (SeriesSource != null && SeriesSource.Any())
            {
                foreach (var series in SeriesSource)
                    BuildInternal(series.AsQueryable(), SkipLevels, NumLevels, null, new ColumnLabel() { Name = series.Key, UniqueID = series.Key });
            }

            if (SeriesQuerySource != null && SeriesQuerySource.Any() && SeriesQuerySource.First().Any())
            {
                foreach (var series in SeriesQuerySource)
                {
                    var selected = series.Select(x => x.Key).ToList();

                    if (selected.Any())
                    {
                        var first = series.First();
                        var source = first.AsQueryable();
                        var key = first.Key;

                        BuildInternal(source, SkipLevels, NumLevels, null, new ColumnLabel() { Name = key, UniqueID = key });

                        Console.WriteLine($"OK {key}");
                    }
                }
            }

            // V3 reports calculate their own total row, thankyewverymuch
            if (!V3)
            {
                CalculateTotalRow(NumLevels - 1);

                if (LeafRowsOnly)
                    PruneToLeafRows();
            }
        }

        private void BuildSingleV3(IQueryable<IReportable> source, ColumnLabel seriescolumn = null)
        {
            IQueryable<IGrouping<object, IReportable>> groups;
            if (WithMonthColumns)
                groups = source.GroupBy(x => new { Name = x.Category, Month = x.Timestamp.Month });
            else
                groups = source.GroupBy(x => new { Name = x.Category });

            var selected = groups.Select(g => new { Key = g.Key, Total = g.Sum(y => y.Amount) });
            BuildPhase_Place(selected, seriescolumn);
        }

        private void BuildPhase_Place(IQueryable<dynamic> selected, ColumnLabel seriescolumn)
        {
            //
            // Phases:
            //  1. Place
            //  2. Propagate
            //  3. Prune
            //

            foreach (var cell in selected)
            {
                string dynamicname = cell.Key.Name;
                var keys = dynamicname.Split(':').Skip(SkipLevels);
                if (keys.Any())
                {
                    // Place each query cell into a report cell
                    var id = string.Join(':', keys) + ":";
                    var name = LeafRowsOnly ? id : null;
                    var row = new RowLabel() { Name = name, UniqueID = id };
                    ColumnLabel column = null;
                    if (WithMonthColumns)
                    {
                        column = new ColumnLabel() { UniqueID = cell.Key.Month.ToString("D2"), Name = new DateTime(2000, cell.Key.Month, 1).ToString("MMM") };
                        base[column, row] += cell.Total;
                    }
                    if (seriescolumn != null)
                        base[seriescolumn, row] += cell.Total;

                    base[TotalColumn, row] += cell.Total;

                    // Propagate totals upward into parent rows which contain totals of all lower-level rows
                    if (!LeafRowsOnly)
                        BuildPhase_Propagate(row: row, column: column, seriescolumn:seriescolumn, amount: cell.Total);
                }
            }

            // Prune needless rows
            if (!LeafRowsOnly)
                BuildPhase_Prune();
        }

        private void BuildPhase_Propagate(decimal amount,RowLabel row, ColumnLabel column = null, ColumnLabel seriescolumn = null)
        {
            var split = row.UniqueID.Split(':');
            var parentsplit = split.SkipLast(1);
            if (parentsplit.Any())
            {
                var parentid = string.Join(':', parentsplit);
                var parentrow = RowLabels.Where(x => x.UniqueID == parentid).SingleOrDefault();
                if (parentrow == null)
                    parentrow = new RowLabel() { Name = parentsplit.Last(), UniqueID = parentid };
                row.Parent = parentrow;

                base[TotalColumn, parentrow] += amount;
                if (column != null)
                    base[column, parentrow] += amount;
                if (seriescolumn != null)
                    base[seriescolumn, parentrow] += amount;

                BuildPhase_Propagate(amount:amount, row:parentrow, column:column, seriescolumn:seriescolumn);

                row.Level = parentrow.Level - 1;
            }
            else
            {
                row.Level = NumLevels - 1;
                base[TotalColumn, TotalRow] += amount;
                if (column != null)
                    base[column, TotalRow] += amount;
                if (seriescolumn != null)
                    base[seriescolumn, TotalRow] += amount;
            }
        }

        private void BuildPhase_Prune()
        {
            // This gets rid of the case where we might have A:B: sitting under A:B, with no other children.
            // In that case, A:B: is duplicating A:B, and is ergo needless
            // Note that this can only be done after ALL the series are in place

            var removeme = new HashSet<RowLabel>();
            foreach (var row in base.RowLabels)
            {
                if (string.IsNullOrEmpty(row.UniqueID.Split(':').Last()))
                    if (row.Parent != null)
                        if (base[TotalColumn, row] == base[TotalColumn, row.Parent as RowLabel])
                            removeme.Add(row);

                // Also prune rows that are below the numrows cutoff
                if (row.Level < 0)
                    removeme.Add(row);
            }

            base._RowLabels.RemoveWhere(x => removeme.Contains(x));
        }

        /// <summary>
        /// Render the report to console
        /// </summary>
        /// <param name="sorted">Whether to show rows in sorted order</param>
        public void WriteToConsole(bool sorted = false)
        {
            if (!RowLabels.Any())
                return;

            var maxlevel = RowLabels.Max(x => x.Level);

            // Columns

            var builder = new StringBuilder();
            var name = string.Empty;
            var padding = (maxlevel > 0) ? String.Concat(Enumerable.Repeat<char>(' ', maxlevel)) : string.Empty;
            builder.Append($"+ {name,15}{padding} ");

            foreach (var col in ColumnLabelsFiltered)
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

                var padding_before = string.Empty; // BROKEN String.Concat( Enumerable.Repeat<char>('>', line.IsTotal ? 0 : maxlevel - line.Level));
                var padding_after = string.Empty; // BROKEN String.Concat( Enumerable.Repeat<char>(' ', line.IsTotal ? maxlevel : line.Level));

                builder.Append($"{line.Level} {padding_before}{name,-15}{padding_after} ");

                foreach (var col in ColumnLabelsFiltered)
                {
                    var val = this[col, line];
                    var format = col.DisplayAsPercent ? "P0" : "C2";
                    builder.Append($"| {val.ToString(format),10} ");
                }

                Console.WriteLine(builder.ToString());
            }
        }

        /// <summary>
        /// Serialize the report to JSON
        /// </summary>
        /// <returns>JSON-encoded string for this report</returns>
        public string ToJson()
        {
            string result = string.Empty;

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream,options:new JsonWriterOptions() { Indented = true }))
                {
                    writer.WriteStartArray();

                    foreach (var line in RowLabelsOrdered)
                    {
                        writer.WriteStartObject();

                        writer.WritePropertyName("Name");
                        writer.WriteStringValue(line.Name ?? "-");
                        writer.WritePropertyName("ID");
                        writer.WriteStringValue(line.UniqueID);
                        writer.WritePropertyName("IsTotal");
                        writer.WriteBooleanValue(line.IsTotal);
                        writer.WritePropertyName("Level");
                        writer.WriteNumberValue(line.Level);

                        foreach (var col in ColumnLabelsFiltered)
                        {
                            var val = this[col, line];
                            var name = col.ToString();
                            if (col.DisplayAsPercent)
                            {
                                val /= 100;
                                name += "%";
                            }
                            writer.WritePropertyName(name);
                            writer.WriteNumberValue(val);
                        }
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();

                    writer.Flush();

                    var bytes = stream.ToArray();
                    result = Encoding.UTF8.GetString(bytes);
                }
            }

            return result;

        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Build the report.
        /// </summary>
        /// <remarks>
        /// This is the heart of this class.
        /// </remarks>
        /// <param name="items">Which items to build into the report</param>
        /// <param name="fromlevel">How many levels deep into the source data do these items start at</param>
        /// <param name="numlevels">How many further levels do render build into the report</param>
        /// <param name="parent">The row which will be the parent of all the rows generated by this call</param>
        /// <param name="seriescolumn">If we're coming from an individual series, where do we accumulate the series total</param>
        void BuildInternal(IQueryable<IReportable> items, int fromlevel, int numlevels, RowLabel parent, ColumnLabel seriescolumn = null)
        {
            var groups = items.GroupBy(x => GetTokenByIndex(x.Category, fromlevel));

            // Skip empty sub-levels unless we're specifically asking for them (LeafRowsOnly)
            if (groups.Count() > 1 || groups.Single().Key != null || LeafRowsOnly) 
            {
                foreach (var group in groups)
                {
                    var token = group.Key;
                    var newpath = parent == null ? token : $"{parent.UniqueID}:{token}";

                    // In case of LeafRowsOnly, we need a fully-qualified name, because headings will be removed.
                    var row = new RowLabel() { Name = LeafRowsOnly?newpath:token, Level = numlevels - 1, UniqueID = newpath, Parent = parent };

                    // Bug 900: Rows out of order on multi-series deep lists
                    if (RowLabels.Contains(row))
                        row = RowLabels.Single(x => x.Equals(row));

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

        /// <summary>
        /// Calculate the total row, from all the columns above
        /// </summary>
        /// <remarks>
        /// We are only totally up the top-level rows, because all the lower-level rows
        /// are accumlated into the top-level rows
        /// </remarks>
        /// <param name="usinglevel">What level are the top-levels at</param>
        void CalculateTotalRow(int usinglevel)
        {
            foreach (var row in base.RowLabels.Where(x => x.Level == usinglevel))
                foreach (var col in base.ColumnLabels)
                    base[col, TotalRow] += base[col, row];
        }

        void PruneToLeafRows()
        {
            // Remove parent rows
            var parents = RowLabels.Where(x => x.Parent != null).Select(x => x.Parent as RowLabel);
            base._RowLabels.RemoveWhere(x=>parents.Contains(x));

            // Adjust remaining rows
            foreach(var row in RowLabels)
                row.Level = 0;
        }

        /// <summary>
        /// Retrieve all the columns for a certain row in a focused way
        /// </summary>
        /// <param name="rowLabel">Which row</param>
        /// <returns>Dictionary of column identifier strings to the value in that column</returns>
        Dictionary<string, decimal> RowDetails(RowLabel rowLabel) => ColumnLabels.ToDictionary(x => x.ToString(), x => base[x, rowLabel]);

        static string GetTokenByIndex(string category, int index)
        {
            if (category == null)
                return null;

            var split = category.Split(':');

            if (index >= split.Count() || split[index] == string.Empty)
                return null;
            else
                return split[index];
        }

        /// <summary>
        /// Row comparer
        /// </summary>
        /// <remarks>
        /// This is the row-sorter. It's somewhat of a challenge to sort a tree of values to sort
        /// within each level but keep the tree sructure intact
        /// </remarks>
        /// <param name="x">First row for comparison</param>
        /// <param name="y">Second row for comparison</param>
        /// <returns>Comparsion value. -1 if first row sorts before second row</returns>
        int IComparer<RowLabel>.Compare(RowLabel x, RowLabel y)
        {
            int result = 0;
            var n = new RowLabel() { Name = "null" };

            // Turn this on if need to trace the sort logic
            bool debugout = false;
            Debug.WriteLineIf(debugout,$"Compare [{x??n}] vs [{y??n}]");

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

            // (2) If these two share a common parent, we can compare based on SortOrder
            if (x.Parent == y.Parent)
            {
                var yval = base[OrderingColumn, y as RowLabel];
                var xval = base[OrderingColumn, x as RowLabel];
                switch (SortOrder)
                {
                    case SortOrders.TotalAscending:
                        Debug.WriteLineIf(debugout, $"Checking Totals: {xval:C2} vs {yval:C2}...");
                        result = yval.CompareTo(xval);
                        break;
                    case SortOrders.TotalDescending:
                        Debug.WriteLineIf(debugout, $"Checking Totals: {xval:C2} vs {yval:C2}...");
                        result = xval.CompareTo(yval);
                        break;
                    case SortOrders.NameAscending:
                        result = x.Name?.CompareTo(y.Name) ?? -1;
                        break;
                }
                goto done;
            }

            // (3) Parents always come before their own children

            if (x.Parent == y)
            {
                Debug.WriteLineIf(debugout, "Parent relationship");
                result = 1;
                goto done;
            }
            else if (y.Parent == x)
            {
                Debug.WriteLineIf(debugout, "Parent relationship");
                result = -1;
                goto done;
            }

            // (4) Search upward if can't resolve at this level

            // If one is deeper than the other, run up that parent chain
            if (x.Level < y.Level)
            {
                Debug.WriteLineIf(debugout, $"Try {x} Parent vs {y}");
                result = ((IComparer<RowLabel>)this).Compare(x.Parent as RowLabel, y);
            }
            else if (y.Level < x.Level)
            {
                Debug.WriteLineIf(debugout, $"Try {x} vs {y} Parent");
                result = ((IComparer<RowLabel>)this).Compare(x, y.Parent as RowLabel);
            }
            else
            {
                // If we're at the SAME level, run both parents upwards
                Debug.WriteLineIf(debugout, $"Try {x} Parent vs {y} Parent");
                result = ((IComparer<RowLabel>)this).Compare(x.Parent as RowLabel, y.Parent as RowLabel);
            }

            if (result == 0)
                Debug.WriteLineIf(debugout, $"??? Unable to resolve");

        done:

            if (result < 0)
                Debug.WriteLineIf(debugout, $">> {x??n} is before {y??n}");
            else if (result > 0)
                Debug.WriteLineIf(debugout, $">> {x??n} is after {y??n}");
            else
                Debug.WriteLineIf(debugout, $">> {x??n} is same as {y??n}");

            return result;
        }

        #endregion
    }

    /// <summary>
    /// Common elements which are shared by both rows and columns
    /// </summary>
    public abstract class BaseLabel: IComparable<BaseLabel>
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

        /// <summary>
        /// Determins whether this instance and the specified object have the same value
        /// </summary>
        /// <param name="obj">Another object for comparison</param>
        /// <returns>True if this instance is the same as the specified object</returns>
        public override bool Equals(object obj)
        {
            return obj is BaseLabel label &&
                   UniqueID == label.UniqueID &&
                   Name == label.Name &&
                   IsTotal == label.IsTotal;
        }

        /// <summary>
        /// Returns the hash code for this object
        /// </summary>
        /// <returns>A 32-bit integer hash code</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(UniqueID, Name, IsTotal);
        }

        /// <summary>
        /// Converts the value of this instance to its equivalent string representation
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            if (IsTotal)
                return "TOTAL";
            if (!string.IsNullOrEmpty(UniqueID))
                return $"ID:{UniqueID}";
            else
                return $"Name:{Name}";
        }

        /// <summary>
        /// Default comparison between base labels
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        int IComparable<BaseLabel>.CompareTo(BaseLabel other)
        {
            int result = IsTotal.CompareTo(other.IsTotal);
            if (result == 0) // Empty orders sort at the END
                result = string.IsNullOrEmpty(UniqueID).CompareTo(string.IsNullOrEmpty(other.UniqueID));
            if (result == 0)
                result = UniqueID?.CompareTo(other.UniqueID) ?? -1;
            if (result == 0)
                result = Name?.CompareTo(other.Name) ?? -1;

            return result;
        }
    }

    /// <summary>
    /// Describes a row
    /// </summary>
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

    /// <summary>
    /// Describes a column 
    /// </summary>
    public class ColumnLabel : BaseLabel
    {
        /// <summary>
        /// Whether this is a calculated column, using 'Custom' property
        /// </summary>
        /// <remarks>
        /// Otherwise it's a regular column with data in the table
        /// </remarks>
        public bool IsCalculated { get; set; } = false;

        /// <summary>
        /// Whether this should be rendered as a percentage
        /// </summary>
        /// <remarks>
        /// Otherwise it's a regular dollar figure
        /// </remarks>
        public bool DisplayAsPercent { get; set; } = false;

        /// <summary>
        /// Custom function which will calculate values for this column based
        /// on values in other columns
        /// </summary>
        public Func<Dictionary<string,decimal>, decimal> Custom { get; set; }
    }
}
