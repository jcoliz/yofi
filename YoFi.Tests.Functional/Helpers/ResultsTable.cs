using Microsoft.Playwright;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YoFi.Tests.Functional.Helpers
{
    /// <summary>
    /// Table of results as extracted from an (e.g.) Index page
    /// </summary>
    /// <remarks>
    /// Expects an element matching selctor "table[data-test-id=results]"
    /// </remarks>
    internal class ResultsTable
    {
        public List<string> Columns { get; } = new List<string>();
        public List<Dictionary<string, string>> Rows { get; } = new List<Dictionary<string, string>>();

        public static async Task<ResultsTable> ExtractResultsFrom(IPage page)
        {
            var table = new ResultsTable();

            var table_el = await page.QuerySelectorAsync("table[data-test-id=results]");
            if (table_el is null)
                return null;

            var headers_el = await table_el.QuerySelectorAllAsync("thead th");
            foreach (var el in headers_el)
            {
                var text = await el.TextContentAsync();
                var header = text.Trim();
                table.Columns.Add(header);
            }

            var rows_el = await table_el.QuerySelectorAllAsync("tbody tr");
            foreach (var row_el in rows_el)
            {
                var row = new Dictionary<string, string>();
                var col_enum = table.Columns.GetEnumerator();
                col_enum.MoveNext();

                var cells_el = await row_el.QuerySelectorAllAsync("td");
                foreach (var cell_el in cells_el)
                {
                    var text = await cell_el.TextContentAsync();
                    var cell = text.Trim();

                    var col = col_enum.Current;
                    if (col != null)
                    {
                        row[col] = cell;
                        col_enum.MoveNext();
                    }
                    else
                    {
                        try
                        {
                            var testid = await cell_el.GetAttributeAsync("data-test-id");
                            var testvalue = await cell_el.GetAttributeAsync("data-test-value");
                            if (testid != null && testvalue != null)
                                row[testid] = testvalue;
                        }
                        catch
                        {
                            // THis is a hack, because I don't know how to test if an element HAS
                            // an attribute or not
                        }
                    }
                }

                table.Rows.Add(row);
            }

            return table;
        }
    }

}
