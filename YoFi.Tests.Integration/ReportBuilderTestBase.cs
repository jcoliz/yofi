using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Reports;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration
{
    public abstract class ReportBuilderTestBase: IntegrationTest
    {
        #region Fields

        protected static readonly CultureInfo culture = new CultureInfo("en-US");

        protected IHtmlDocument document;
        protected string h2 = default;
        protected IDictionary<string,IElement> tables;

        protected string testid { get; set; }
        protected IElement table => tables[testid];
        protected string total => table.QuerySelector("tr.report-row-total td.report-col-total")?.TextContent.Trim();
        protected IEnumerable<IElement> cols => table.QuerySelectorAll("th").Skip(1);
        protected IHtmlCollection<IElement> rows => table.QuerySelectorAll("tbody tr");

        #endregion

        #region Init/Cleanup

        protected void Cleanup()
        {
            tables = null;
            testid = null;
        }

        #endregion

        #region Helpers

        protected async Task WhenGettingReport(string url)
        {
            // First get the outer layout
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            // Then find the url to the inner report
            document = await parser.ParseDocumentAsync(await response.Content.ReadAsStreamAsync());
            var element = document.QuerySelector("div.loadr");
            var endpoint = element.GetAttribute("data-endpoint")?.Trim();

            // Finally, get the inner report
            await WhenGettingReportDirectly(endpoint);
        }

        protected async Task WhenGettingReportDirectly(string url)
        {
            var response = await client.GetAsync(url);

            // Then: It's OK
            response.EnsureSuccessStatusCode();

            // And: Parse it into components
            document = await parser.ParseDocumentAsync(await response.Content.ReadAsStreamAsync());
            h2 = document.QuerySelector("H2")?.TextContent.Trim();
            tables = document.QuerySelectorAll("table").ToDictionary(x => x.GetAttribute("data-test-id").Trim(), x => x);
            if (tables.Count() == 1)
                testid = tables.Single().Key;
        }

        protected async Task WhenGettingReport(ReportParameters parameters)
        {
            // Given: A URL which encodes these report parameters

            var start = (parameters.id == "summary")
                ? $"/Reports/?year={parameters.year}"
                : $"/Report/{parameters.id}?year={parameters.year}";

            var builder = new StringBuilder(start);

            if (parameters.showmonths.HasValue)
                builder.Append($"&showmonths={parameters.showmonths}");
            if (parameters.month.HasValue)
                builder.Append($"&month={parameters.month}");
            if (parameters.level.HasValue)
                builder.Append($"&level={parameters.level}");

            var url = builder.ToString();

            // When: Getting a report at that URL from the system
            if (parameters.id == "summary")
                await WhenGettingReportDirectly(url);
            else
                await WhenGettingReport(url);

            // Then: Is showing the correct report
            if (tables.Count() == 1)
                Assert.AreEqual($"report-{parameters.id}", testid);

            // And: Return to the caller for futher checks
        }

        protected void ThenReportHasTotal(decimal expected, string report = null)
        {
            if (!(report is null))
                testid = $"report-{report}";

            Assert.AreEqual(expected.ToString("C0", culture), total);
        }

        protected string GetCell(string col, string row)
        {
            // Note that finding an arbitrary cell in the table is more involved. I didn't want
            // to mark up EVERY cell with a data-test-id. Instead I marked up the headers. So I
            // need to figure out which column## has the data I want, and then look for it in
            // the row cols

            var index = cols.Index(cols.Where(x => x.GetAttribute("data-test-id") == $"col-{col}").Single());
            var result = table.QuerySelectorAll($"tr[data-test-id=row-{row}] td.report-col-amount")[index].TextContent.Trim();

            return result;
        }

        #endregion    
    }
}
