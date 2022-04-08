using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YoFi.Core.Reports;

namespace YoFi.Tests.Helpers.ReportExtensions
{
    internal static class ReportWriteExtensions
    {
        /// <summary>
        /// Render the report to console
        /// </summary>
        /// <param name="sorted">Whether to show rows in sorted order</param>
        public static void WriteToConsole(this Report report, bool sorted = false) => report.Write(Console.Out, sorted);

        /// <summary>
        /// Render the report to the given <paramref name="writer"/>
        /// </summary>
        /// <param name="writer">Where to write</param>
        /// <param name="sorted">Whether to show rows in sorted order</param>
        public static void Write(this Report report, TextWriter writer, bool sorted = false)
        {
            if (!report.RowLabels.Any())
                return;

            var maxlevel = report.RowLabels.Max(x => x.Level);

            // Columns

            var builder = new StringBuilder();
            var name = report.Name ?? string.Empty;
            var padding = (maxlevel > 0) ? String.Concat(Enumerable.Repeat<char>(' ', maxlevel)) : string.Empty;
            builder.Append($"+ {name,25}{padding} ");

            foreach (var col in report.ColumnLabelsFiltered)
            {
                name = col.Name;
                if (col.IsTotal)
                    name = "TOTAL";
                builder.Append($"| {name,13} ");
            }

            writer.WriteLine(builder.ToString());

            // Rows

            var rows = sorted ? report.RowLabelsOrdered : report.RowLabels;
            foreach (var row in rows)
            {
                builder = new StringBuilder();

                name = row.Name;
                if (row.IsTotal)
                    name = "TOTAL";
                if (name == null)
                    name = "-";

                var padding_before = string.Concat(Enumerable.Repeat<char>('>', row.IsTotal ? 0 : maxlevel - row.Level));
                var padding_after = string.Concat(Enumerable.Repeat<char>(' ', row.IsTotal ? maxlevel : row.Level));

                builder.Append($"{row.Level} {padding_before}{name,-25}{padding_after} ");

                foreach (var col in report.ColumnLabelsFiltered)
                {
                    var val = report[col, row];
                    var format = col.DisplayAsPercent ? "P0" : "C2";
                    builder.Append($"| {val.ToString(format),13} ");
                }

                builder.Append($" {row}");

                writer.WriteLine(builder.ToString());
            }
        }

    }
}
