using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace YoFi.Tests.Functional;

[TestClass]
public class AdminUITest: FunctionalUITest
{
    [TestMethod]
    public async Task SeedDatabase_Bug1387()
    {
        //
        // Bug 1387: [Production Bug] Seed database with transactions does not save splits
        //
        // Given: On Admin Page
        // And: Having deleted transactions
        // When: Seeting database with "Transactions Full Year"
        // And: Viewing Income Report
        // Then: $149k of income
        //
        // If there is less income it's because the splits did not get seeded correctly.
        //

        // Given: On Admin Page
        await WhenNavigatingToPage("Admin");
        await Page.SaveScreenshotToAsync(TestContext);

        // And: Having deleted transactions
        await Page.ClickAsync("button[data-id=\"tx\"]");
        await Task.Delay(500);
        await Page.SaveScreenshotToAsync(TestContext);
        await Page.ClickAsync("data-test-id=btn-modal-ok");
        await Page.SaveScreenshotToAsync(TestContext);

        // When: Seeding database with "Transactions Full Year"
        await Page.ClickAsync("div[data-id=\"txyear\"]");
        await Task.Delay(500);
        await Page.SaveScreenshotToAsync(TestContext);
        await Page.ClickAsync("text=Close");
        await Page.SaveScreenshotToAsync(TestContext);

        // And: Navigating to Income Page
        await WhenNavigatingToPage("Reports");
        await Page.SaveScreenshotToAsync(TestContext);

        // Then: Income is $149,000
        var text = await Page.TextContentAsync("table[data-test-id=report-income] >> tr.report-row-total >> td.report-col-total");
        var trimmed = text.Trim();
        Assert.AreEqual("$149,000",trimmed);
    }
}