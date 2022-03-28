using Microsoft.Playwright;
using System.Collections.Generic;
using System.Linq;
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
            var table_el = page.Locator("table[data-test-id=results]");
            if (!await table_el.IsVisibleAsync())
                return null;

            return await ExtractResultsFrom(table_el);
        }
        public static async Task<ResultsTable> ExtractResultsFrom(ILocator table_el)
        {
            var table = new ResultsTable();

            var headers_el = table_el.Locator("thead th");

            var headercount = await headers_el.CountAsync();

            for (var h = 0; h < headercount; ++h)
            {
                var header_el = headers_el.Nth(h);
                var testid = await header_el.GetAttributeAsync("data-test-id");
                if (testid != null)
                {
                    table.Columns.Add(testid);
                }
                else
                {
                    var text = await header_el.TextContentAsync();
                    if (!string.IsNullOrEmpty(text))
                        table.Columns.Add(text.Trim());
                }
            }

            var rows_el = table_el.Locator("tbody tr");

            var count = await rows_el.CountAsync();
            for (var i = 0; i < count; ++i)
            {
                var row_el = rows_el.Nth(i);
                var cells_el = row_el.Locator("td");

                var row = new Dictionary<string, string>();
                var col_enum = table.Columns.GetEnumerator();
                col_enum.MoveNext();

                var cellcount = await cells_el.CountAsync();

                for (var c = 0; c < cellcount; ++c)
                {
                    var cell_el = cells_el.Nth(c);

                    var col = col_enum.Current;
                    if (col != null)
                    {
                        string testvalue = null;
                        try
                        {
                            testvalue = await cell_el.GetAttributeAsync("data-test-value");
                        }
                        catch
                        {

                        }
                        if (!string.IsNullOrEmpty(testvalue))
                        {
                            row[col] = testvalue;
                        }
                        else
                        {
                            row[col] = (await cell_el.TextContentAsync()).Trim();
                        }
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
