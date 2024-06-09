using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using YoFi.Core.Models;

namespace YoFi.Core.Reports;

/// <summary>
/// Table-based report: Arranges IReportable items into tables
/// </summary>
/// <remarks>
/// General usage: 
/// 1. Set the Source
/// 2. Set any needed configuration properties
/// 3. Build the report
/// 4. Iterate over rows/columns to display the report
/// 
/// Note that there is no app-specific logic of any form in this
/// class.
/// </remarks>
public class Report : IDisplayReport, IComparer<RowLabel>
{
    #region Configuration Properties

    /// <summary>
    /// Display name of the report
    /// </summary>
    public string Name { get; set; } = "Report";

    /// <summary>
    /// In-depth description of the report for display
    /// </summary>
    /// <remarks>
    /// In practice, this is used exclusively to describe the timeframe covered by
    /// the report, so could be renamed "timeframe"
    /// </remarks>
    public string Description { get; set; }

    /// <summary>
    /// The definition ID used to generate this report
    /// </summary>
    public string Definition { get; set; }

    /// <summary>
    /// Where to get items
    /// </summary>
    /// <remarks>
    /// Multiple queries can have the same name, in which case they
    /// are added together as if we concatenated the queries
    /// </remarks>
    public IEnumerable<NamedQuery> Source { get; set; }

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
    /// <remarks>
    /// Set this to null to include all available levels.
    /// Build() will update this value to how many levels were found
    /// </remarks>
    public int? NumLevels { get; set; } = 1;

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
            if (WithTotalColumn)
                return TotalColumn;

            return ColumnLabels.First();
        }
    }

    #endregion

    #region Informative Properties

    /// <summary>
    /// Indexer to retrieve report cells
    /// </summary>
    /// <param name="column">Which column</param>
    /// <param name="row">Which row</param>
    /// <returns>Value at that <paramref name="column"/>,<paramref name="row"/> position </returns>
    public decimal this[ColumnLabel column, RowLabel row]
    {
        get
        {
            if (column.IsCalculated)
            {
                var cols = RowDetails(row);
                // Add in the grand total, because some column calculators need access to it
                cols["GRANDTOTAL"] = GrandTotal;
                return column.Custom(cols);
            }
            else
                return Table[column, row];
        }
    }

    /// <summary>
    /// All the row labels, in the correct order as specified by SortOrder
    /// </summary>
    public IEnumerable<RowLabel> RowLabelsOrdered => Table.RowLabels.OrderBy(x => x, this);

    /// <summary>
    /// All the row labels, in internal order
    /// </summary>
    public IEnumerable<RowLabel> RowLabels => Table.RowLabels.OrderBy(x => x);

    /// <summary>
    /// All the column labels which should be shown, as modified by configuration properties
    /// </summary>
    public IEnumerable<ColumnLabel> ColumnLabelsFiltered => ColumnLabels.Where(x => WithTotalColumn || !x.IsTotal);

    /// <summary>
    /// All the column labels, in internal order
    /// </summary>
    public IEnumerable<ColumnLabel> ColumnLabels => Table.ColumnLabels.OrderBy(x => x);

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
    /// How far through the year does this report go?
    /// </summary>
    /// <remarks>
    /// This only applies for reports who have YearProgress=true in their report definition.
    /// Currently, this is calculated by the report builder. 
    /// TODO: Better is to calculate it here in this clasee.
    /// </remarks>
    public double YearProgress { get; set; }

    /// <summary>
    /// The maximum number of levels this report could have been built with
    /// </summary>
    /// <remarks>
    /// Filled in after building
    /// </remarks>
    public int MaxLevels { get; set; }

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
        Table.ColumnLabels.Add(column);
    }

    /// <summary>
    /// Load configuration parameters from supplied <paramref name="definition"/>
    /// </summary>
    /// <param name="definition"></param>
    public void LoadFrom(ReportDefinition definition)
    {
        if (!string.IsNullOrEmpty(definition.Name))
            Name = definition.Name;

        if (!string.IsNullOrEmpty(definition.SortOrder) && Enum.TryParse<Report.SortOrders>(definition.SortOrder, out Report.SortOrders order))
            SortOrder = order;

        if (definition.WithMonthColumns.HasValue)
            WithMonthColumns = definition.WithMonthColumns.Value;

        if (definition.WithTotalColumn.HasValue)
            WithTotalColumn = definition.WithTotalColumn.Value;

        if (definition.SkipLevels.HasValue)
            SkipLevels = definition.SkipLevels.Value;

        if (definition.NumLevels.HasValue)
            NumLevels = definition.NumLevels.Value;

        if (definition.DisplayLevelAdjustment.HasValue)
            DisplayLevelAdjustment = definition.DisplayLevelAdjustment.Value;

        if (!string.IsNullOrEmpty(definition.slug))
            Definition = definition.slug;
    }

    /// <summary>
    /// Build the report
    /// </summary>
    public void Build()
    {
        if (NumLevels.HasValue && NumLevels.Value < 1)
            throw new ArgumentOutOfRangeException(nameof(NumLevels), "Must be 1 or greater or null");

        if (Source == null)
            throw new ArgumentOutOfRangeException(nameof(Source), "Must set a source");

        // Build each query individually
        foreach (var query in Source)
            BuildPhase_Group(query);
    }

    /// <summary>
    /// Produce a report which is a slice of this report, including everything below
    /// the supplied <paramref name="rowname"/>
    /// </summary>
    /// <remarks>
    /// The use case for taking a slice of an existing report is that it allows you to
    /// just run all the queries once for a master report, then break it up into
    /// component pieces for individual display
    /// </remarks>
    /// <param name="rowname">Name of current row to use as root</param>
    /// <returns>New report</returns>
    public Report TakeSlice(string rowname)
    {
        var result = new Report() { Name = rowname, Description = Description };

        // Find the row
        var findrow = RowLabels.Where(x => x.Name == rowname && !x.IsTotal);
        if (!findrow.Any())
            return result;

        var sliceparent = findrow.Single();

        // Find the rows constituting the slice
        var includedrows = RowLabels.Where(x => x.DescendsFrom(sliceparent)).ToList();

        // Bring the columns of the old report over
        foreach (var column in ColumnLabelsFiltered)
            if (!column.IsCalculated)
            {
                foreach (var row in includedrows)
                {
                    var value = this[column, row];
                    result.Table[column, row] = value;

                    if (row.Parent == sliceparent)
                        result.Table[column, TotalRow] += value;
                }
            }
            else
                result.AddCustomColumn(column);

        result.NumLevels = NumLevels - 1;

        return result;
    }

    /// <summary>
    /// Produce a report which is a slice of this report, excluding everything below
    /// the supplied <paramref name="rownames"/>
    /// </summary>
    /// <remarks>
    /// The use case for taking a slice of an existing report is that it allows you to
    /// just run all the queries once for a master report, then break it up into
    /// component pieces for individual display
    /// </remarks>
    /// <param name="rownames">Name of top-level rows to exclude</param>
    /// <returns>New report</returns>
    public Report TakeSliceExcept(IEnumerable<string> rownames)
    {
        var result = new Report();

        // Find the parent rows to exclude
        var excludedparentrows = RowLabels.Where(x => rownames.Contains(x.Name) && x.Parent == null && !x.IsTotal);

        // Find all the rows (parent and child) to exclude
        var excluded = excludedparentrows.SelectMany(x => RowLabels.Where(y => y.IsTotal || y.Equals(x) || y.DescendsFrom(x)));

        // Find the rows constituting the slice
        var includedrows = RowLabels.Except(excluded);

        // Bring the columns of the old report over
        foreach (var column in ColumnLabelsFiltered)
            if (!column.IsCalculated)
            {
                foreach (var row in includedrows)
                {
                    var value = this[column, row];
                    result.Table[column, row] = value;

                    if (row.Parent == null && includedrows.Contains(row))
                        result.Table[column, TotalRow] += value;
                }
            }
            else
                result.AddCustomColumn(column);

        result.Name = string.Join(',',rownames);
        result.NumLevels = NumLevels;

        return result;
    }

    /// <summary>
    /// Prune off levels below <paramref name="newlevel"/>
    /// </summary>
    /// <param name="newlevel">Resulting value of NumLevels after operation is complete</param>
    public void PruneToLevel(int newlevel)
    {
        if (!NumLevels.HasValue)
            throw new ApplicationException("Report has no set level");

        if (newlevel < 1 || newlevel > NumLevels)
            throw new ArgumentException("Invalid level", nameof(newlevel));

        var minuslevels = NumLevels.Value - newlevel;
        foreach (var row in RowLabels.Where(x=>!x.IsTotal))
            row.Level -= minuslevels;

        Table.RowLabels.RemoveWhere(x => x.Level < 0);

        NumLevels = newlevel;
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
            using var writer = new Utf8JsonWriter(stream, options: new JsonWriterOptions() { Indented = true });
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
                        name += "%";

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

        return result;

    }

    #endregion

    #region Fields
    readonly Table<ColumnLabel, RowLabel, decimal> Table = new();
    #endregion

    #region Internal Methods
    
    /// <summary>
    /// This is the object which we use as the grouping key for IReportable items
    /// </summary>
    class CellGroupDto
    {
        public string Row { get; set; }
        public int? Column { get; set; } = null;

        public string FilteredRowName => string.IsNullOrEmpty(Row) ? "[Blank]" : Row;
    };

    /// <summary>
    /// This holds the sum total of all items in a grouping, which is in fact
    /// the location (key) and value of a cell.
    /// </summary>
    class CellTotalDto
    {
        public CellGroupDto Location { get; set; }

        public decimal Value { get; set; }
    };

    /// <summary>
    /// Group source transactions by name/month and calculate totals, and call remaining
    /// phases.
    /// </summary>
    /// <remarks>
    /// Phases:
    ///  1. Group. Group source transactions by name/month and calculate totals
    ///  2. Place. Place each incoming data point into a report cell.
    ///  3. Propagate. Propagate those values upward into totalling rows.
    ///  4. Prune. Prune out rows that are not really needed.
    /// </remarks>
    /// <param name="source">Source of reportables, with optional series name key</param>

    private void BuildPhase_Group(NamedQuery source)
    {
        IQueryable<IGrouping<CellGroupDto, IReportable>> groups;
        if (WithMonthColumns)
            groups = source.Query.GroupBy(x => new CellGroupDto() { Row = x.Category, Column = x.Timestamp.Month });
        else
            groups = source.Query.GroupBy(x => new CellGroupDto() { Row = x.Category });

        //  1. Group. Group source transactions by name/month and calculate totals
        // TODO: QueryExec SumAsync
        var selected = groups.Select(g => new CellTotalDto() { Location = g.Key, Value = g.Sum(y => y.Amount) });

        //  2. Place. Place each incoming data point into a report cell.
        BuildPhase_Place(cells: selected, oquery: source);

        // Update collector row labels
        CollectorRows = RowLabels.Where(x => x.Collector != null).ToLookup(x => x.UniqueID.Count(y => y == ':'));
    }

    /// <summary>
    /// Collection of rows which collect the totals of other rows
    /// </summary>
    private ILookup<int,RowLabel> CollectorRows = null;

    /// <summary>
    /// Pre-calculated set of labels for month columns
    /// </summary>
    private static Dictionary<int,ColumnLabel> MonthColumnLabels { get; } = Enumerable.Range(1,12).ToDictionary(x=>x,x=>new ColumnLabel() 
        { 
            UniqueID = x.ToString("D2"), 
            Name = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(x)
        }
    );

    /// <summary>
    /// Place data into report, and call remaining phases
    /// </summary>
    /// <remarks>
    /// The heart of V3 reports is to push the summing of items into the server side.
    /// This method is the first place where data is actually brought into the client
    /// and worked on.
    /// </remarks>
    /// <param name="cells">Report data</param>
    /// <param name="oquery">Original query</param>
    private void BuildPhase_Place(IQueryable<CellTotalDto> cells, NamedQuery oquery)
    {
        // If this query has a NAME, then we are also collecting the values in a
        // series column.
        var seriescolumn = string.IsNullOrEmpty(oquery?.Name) ? 
            null : 
            new ColumnLabel() 
            { 
                Name = oquery.Name, 
                UniqueID = oquery.Name, 
                LeafNodesOnly = oquery.LeafRowsOnly,
                IsSeries = true
            };

        // Make sure the series column is in the report, even if a total is never entered
        if (null != seriescolumn)
            Table.ColumnLabels.Add(seriescolumn);

        // The pattern for collector rows
        var collectorregex = new Regex("(.+?)\\[(.+?)\\]");

        //  2. Place. Place each incoming data point into a report cell.
        foreach (var cell in cells)
        {
            var keysplit = cell.Location.FilteredRowName.Split(':');
            if (keysplit.Length == SkipLevels)
                keysplit = keysplit.ToList().Append("[Blank]").ToArray();
            var keys = keysplit.Skip(SkipLevels).ToList();
            if (keys.Any())
            {
                // Is this a collector row?
                var match = collectorregex.Match(keys.Last());
                string collector = null;
                if (match.Success)
                {
                    // Replace the last key with only the first part of the match
                    keys[keys.Count-1] = match.Groups[1].Value;

                    // The second part IS the collector rule
                    collector = match.Groups[2].Value;
                }

                // Reconstitute the category from parts
                var id = string.Join(':', keys) + (!oquery.LeafRowsOnly ? ":" : string.Empty);

                // Leaf-rows reports use the ID as a name.
                var name = oquery.LeafRowsOnly ? id : null;

                // Does this row already exist?
                var row = RowLabels.Where(x => x.UniqueID == id).SingleOrDefault();

                // If not, we'll go ahead and create one now
                if (row == null)
                    row = new RowLabel() { Name = name, UniqueID = id, Collector = collector };

                // Place the value in the correct month column if applicable
                ColumnLabel monthcolumn = null;
                if (WithMonthColumns && cell.Location.Column.HasValue)
                {
                    monthcolumn = MonthColumnLabels[cell.Location.Column.Value];
                    Table[monthcolumn, row] += cell.Value;
                }

                // Place the value in the correct series column, if applicable
                if (seriescolumn != null)
                    Table[seriescolumn, row] += cell.Value;

                // Place the value in the total column
                Table[TotalColumn, row] += cell.Value;

                // 3. Propagate. Propagate those values upward into totalling rows.
                // By definition "leaf rows only" means DON'T propagate into totalling rows
                if (!oquery.LeafRowsOnly)
                    BuildPhase_Propagate(row: row, column: monthcolumn, seriescolumn: seriescolumn, amount: cell.Value);
            }
        }

        //  4. Prune. Prune out rows that are not really needed.
        BuildPhase_Prune();
    }

    /// <summary>
    /// Propagate values upward to be collected by all generations of parents
    /// </summary>
    /// <param name="amount">The amount value for this cell</param>
    /// <param name="row">The current row, upward from which we are going to propagate</param>
    /// <param name="column">The month column where this cell exists, or null</param>
    /// <param name="seriescolumn">The series column which is collecting series totals, or null if there isn't one</param>
    private void BuildPhase_Propagate(decimal amount, RowLabel row, ColumnLabel column = null, ColumnLabel seriescolumn = null)
    {
        // Who is our parent? Based on ID. e.g. if we are A:B:C, our parent is A:B
        var split = row.UniqueID.Split(':');
        var parentsplit = split.SkipLast(1);

        // If we DO have a parent...
        if (parentsplit.Any())
        {
            // Find the actual parent ROW, given that ID. Or create if not exists
            var parentid = string.Join(':', parentsplit);

            var parentrow = new RowLabel() { Name = parentsplit.Last(), UniqueID = parentid };
            if (Table.RowLabels.TryGetValue(parentrow, out var foundrow))
                parentrow = foundrow;
            row.Parent = parentrow;

            // Do the accumlation of values into the parent row
            Table[TotalColumn, parentrow] += amount;
            if (column != null)
                Table[column, parentrow] += amount;
            if (seriescolumn != null)
                Table[seriescolumn, parentrow] += amount;

            // Then recursively accumulate upwards to the grandparent
            BuildPhase_Propagate(amount: amount, row: parentrow, column: column, seriescolumn: seriescolumn);

            // Now that the parent has been propagated, we finally know what level WE are, so we can set it here
            row.Level = parentrow.Level - 1;

            // User Story 819: Managed Budget Report
            // There is also a case where a sibling wants to collect our values also. We will deal with that here.
            // A valid peer-collection row will have an ID like A:B:X[^C], which collects all A:B children except
            // A:B:C, into a row named A:B:X.
            // The peer-collection row will be in place before we get here. So if the peer-collector is not
            // found, we are not peer-collecting.
            // Peer collection is (currently) only done for series values.
            if (seriescolumn != null && CollectorRows != null && CollectorRows.Any())
            {
                // Let's find any peer-collecting rows where we share a common category parent
                var peerstart = parentid;

                var depth = 1 + parentid.Count(x => x == ':');
                var collectors = CollectorRows[depth];
                var foundrows = collectors.Where(x => x.UniqueID.StartsWith(parentid));

                // Consider each peer-collector to see if it collects US
                foreach(var collectorrow in foundrows)
                {
                    var rule = collectorrow.Collector;
                    var isnotlist = rule.StartsWith('^');
                    if (isnotlist)
                        rule = rule[1..];
                    var categories = rule.Split(';');

                    // When 'isnotlist' is false, the catgories array contains categories
                    // we DO match. When it's true, the opposite is true.
                    if (categories.Contains(split.Last()) ^ isnotlist && collectorrow.UniqueID != row.UniqueID)
                    {
                        // Found a peer collector who wants us, let's do the collection.
                        Table[seriescolumn, collectorrow] += amount;
                    }
                }
            }
        }
        // Else we are a top-level row
        else
        {
            // In this case, we accumulate into the report total, for all applicable columns
            Table[TotalColumn, TotalRow] += amount;
            if (column != null)
                Table[column, TotalRow] += amount;
            if (seriescolumn != null)
                Table[seriescolumn, TotalRow] += amount;

            // We now know what level we are, so we can set it here.
            if (NumLevels.HasValue)
                row.Level = NumLevels.Value - 1;
        }
    }

    private void BuildPhase_Prune()
    {
        // This gets rid of the case where we might have A:B: sitting under A:B, with no other children.
        // In that case, A:B: is duplicating A:B, and is ergo needless
        // Note that this can only be done after ALL the series are in place

        // Also will handle the case where one (or more) series in the report is "leaf nodes only"
        // while the others were propagated. In this case, we will prune out rows which don't have
        // a value in leaf-nodes-only series
        var leafnodecolumns = ColumnLabelsFiltered.Where(x => x.LeafNodesOnly);

        // We have to prune in two phases now, because the report can have levels automatically
        // set. However, we need to do the level-finding calculation AFTER the first round of
        // pruning

        var pruned = new HashSet<RowLabel>();
        foreach (var row in RowLabels)
        {
            if (!leafnodecolumns.Any())
                if (string.IsNullOrEmpty(row.UniqueID.Split(':').Last()))
                    if (row.Parent != null)
                        if (Table[TotalColumn, row] == Table[TotalColumn, row.Parent as RowLabel])
                            pruned.Add(row);

        }
        Table.RowLabels.RemoveWhere(x => pruned.Contains(x));

        // This is the time to set maxlevels
        if (RowLabels.Any())
        {
            var minrow = RowLabels.Min(x => x.Level);
            MaxLevels = 1 - minrow;

            // In the case of an "all" levels report, this is the time to handle that
            if (!NumLevels.HasValue && RowLabels.Any())
            {
                NumLevels = MaxLevels;
                if (NumLevels > 1)
                {
                    foreach (var row in RowLabels.Where(x => !x.IsTotal))
                        row.Level += NumLevels.Value - 1;
                }
            }
        }
        else
            MaxLevels = 1;

        pruned = new HashSet<RowLabel>();
        foreach (var row in RowLabels)
        {
            // Also prune rows that are below the numrows cutoff
            if (row.Level < 0)
                pruned.Add(row);

            // Also prune rows that aren't included a leaf-rows-only series, IF there is at least one
            // such series. Also flatten series if there is a leaf-rows-only series.
            if (leafnodecolumns.Any())
            {
                if (leafnodecolumns.Sum(x => Table[x, row]) == 0)
                    pruned.Add(row);
                else
                {
                    row.Level = 0;
                    row.Parent = null;
                }
            }
        }

        Table.RowLabels.RemoveWhere(x => pruned.Contains(x));
    }

    /// <summary>
    /// Retrieve all the columns for a certain row in a focused way
    /// </summary>
    /// <param name="rowLabel">Which row</param>
    /// <returns>Dictionary of column identifier strings to the value in that column</returns>
    Dictionary<string, decimal> RowDetails(RowLabel rowLabel) => ColumnLabels.ToDictionary(x => x.ToString(), x => Table[x, rowLabel]);

    /// <summary>
    /// Row comparer
    /// </summary>
    /// <remarks>
    /// This is the row-sorter. It's somewhat of a challenge to sort a tree of values to sort
    /// within each level but keep the tree sructure intact
    /// </remarks>
    /// <param name="first">First row for comparison</param>
    /// <param name="second">Second row for comparison</param>
    /// <returns>Comparsion value. -1 if <paramref name="first"/> sorts before <paramref name="second"/></returns>
    int CompareRows(RowLabel first, RowLabel second)
    {
        // (1) Total rows always come after other rows
        if (first.IsTotal && second.IsTotal)
            return 0;
        else if (first.IsTotal)
            return 1;
        else if (second.IsTotal)
            return -1;

        // (2) Parents always come before their own children
        if (first.Parent == second)
            return 1;
        else if (second.Parent == first)
            return -1;

        // (3) If these two share a common parent, we can compare based on SortOrder
        if (first.Parent == second.Parent)
        {
            var secondval = Table[OrderingColumn, second];
            var firstval = Table[OrderingColumn, first];
            switch (SortOrder)
            {
                case SortOrders.TotalAscending:
                    return secondval.CompareTo(firstval);
                case SortOrders.TotalDescending:
                    return firstval.CompareTo(secondval);
                case SortOrders.NameAscending:
                    return first.Name?.CompareTo(second.Name) ?? -1;
            }
        }

        // (4) Search upward if can't resolve at this level

        // If one is deeper than the other, run up that parent chain
        // Or if not, we're at the SAME level, run both parents upwards
        if (first.Level < second.Level)
            return CompareRows(first.Parent, second);
        else if (second.Level < first.Level)
            return CompareRows(first, second.Parent);
        else
            return CompareRows(first.Parent, second.Parent);
    }

    /// <summary>
    /// Row comparer for use by IComparer
    /// </summary>
    /// <see cref="Report.CompareRows(RowLabel, RowLabel)"/>
    /// <param name="first">First row for comparison</param>
    /// <param name="second">Second row for comparison</param>
    /// <returns>Comparsion value. -1 if <paramref name="first"/> sorts before <paramref name="second"/></returns>
    int IComparer<RowLabel>.Compare(RowLabel first, RowLabel second) => CompareRows(first, second);

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
    /// Final total-displaying row or column
    /// </summary>
    public bool IsTotal { get; set; }

    /// <summary>
    /// True if this row or column should sort AFTER the totals
    /// </summary>
    public bool IsSortingAfterTotal { get; set; }

    /// <summary>
    /// Determins whether this instance and the specified object have the same value
    /// </summary>
    /// <param name="obj">Another object for comparison</param>
    /// <returns>True if this instance is the same as the specified object</returns>
    public override bool Equals(object obj)
    {
        return obj is BaseLabel label
            && ToString().Equals(label.ToString());
    }

    /// <summary>
    /// Returns the hash code for this object
    /// </summary>
    /// <returns>A 32-bit integer hash code</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(ToString());
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
        if (!string.IsNullOrEmpty(Name))
            return $"Name:{Name}";
        return "-";
    }

    /// <summary>
    /// Default comparison between base labels
    /// </summary>
    /// <param name="other">Another base label for comparison</param>
    /// <returns></returns>
    public int CompareTo(BaseLabel other)
    {
        bool thisidnull = string.IsNullOrEmpty(UniqueID);
        bool otheridnull = string.IsNullOrEmpty(other.UniqueID);
        bool thisnamenull = string.IsNullOrEmpty(Name);
        bool othernamenull = string.IsNullOrEmpty(other.Name);

        int result = IsSortingAfterTotal.CompareTo(other.IsSortingAfterTotal);
        if (result == 0)
            result = IsTotal.CompareTo(other.IsTotal);
        if (result == 0) // Empty orders sort at the END
            result = thisidnull.CompareTo(otheridnull);
        if (result == 0 && !thisidnull)
            result = UniqueID.CompareTo(other.UniqueID);
        if (result == 0) 
            result = thisnamenull.CompareTo(othernamenull);
        if (result == 0 && !thisnamenull)
            result = Name.CompareTo(other.Name);

        return result;
    }       
}

/// <summary>
/// Describes a row
/// </summary>
public class RowLabel: BaseLabel
{
    /// <summary>
    /// In a multi-level report, whom are we under? or null for top-level
    /// </summary>
    [JsonIgnore]
    public RowLabel Parent { get; set; }

    /// <summary>
    /// How many levels ABOVE regular data is this?
    /// </summary>
    /// <remarks>
    /// Regular, lowest data is Level 0. First heading above that is Level 1,
    /// next heading up is level 2, etc.
    /// </remarks>
    public int Level { get; set; }

    /// <summary>
    /// If set, this row collects values from peer-level rows under
    /// the same parent, according to the rule it specifies. This is
    /// a semicolon-separated series of category values, optionally
    /// with a '^' to start. The caret indicates we should collect
    /// from all peers EXCEPT those in the list.
    /// </summary>
    public string Collector { get; set; } = null;

    /// <summary>
    /// Determine whether this row has the specified <paramref name="ancestor"/> in
    /// its line of parentage
    /// </summary>
    /// <param name="ancestor">The row to search for</param>
    /// <returns>True if we are some level of child from the specified <paramref name="ancestor"/></returns>
    public bool DescendsFrom(RowLabel ancestor)
    {
        if (Parent == ancestor)
            return true;
        else if (Parent == null)
            return false;
        else
            return Parent.DescendsFrom(ancestor);
    }

    /// <summary>
    /// Universal total row
    /// </summary>
    public static readonly RowLabel Total = new() { IsTotal = true };
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
    /// True if this column should not be propagating values up to parent rows
    /// </summary>
    public bool LeafNodesOnly { get; set; } = false;

    /// <summary>
    /// True if this column represents a distinct series of data
    /// </summary>
    public bool IsSeries { get; set; } = false;

    /// <summary>
    /// Custom function which will calculate values for this column based
    /// on values in other columns
    /// </summary>
    [JsonIgnore]
    public Func<Dictionary<string,decimal>, decimal> Custom { get; set; }

    /// <summary>
    /// Universal total column
    /// </summary>
    public static readonly ColumnLabel Total = new() { IsTotal = true };

    /// <summary>
    /// Rendering priority order
    /// </summary>
    /// <remarks>
    /// Lower numbers are rendered before higher numbers.
    /// 
    /// This is to handle the case where our rendering display doesn't have enough
    /// width to show all columns. e.g. if you can only display 3 cols, display three
    /// with the lowest 'Priority' values
    /// </remarks>
    public int Priority
    {
        get
        {
            if (IsTotal)
                return 1;
            if (IsSeries)
                return 2;
            if (DisplayAsPercent)
                return 3;
            if (IsSortingAfterTotal)
                return 4;

            // At this point we know it's a month column. Later months have higher priority

            if (int.TryParse(UniqueID,out var month))
            {
                return 20 - month;
            }

            // This shouldn't happen
            return 100;
        }
    }

}
