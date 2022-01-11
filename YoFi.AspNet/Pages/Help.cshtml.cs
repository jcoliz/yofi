using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.DotNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace YoFi.AspNet.Pages
{
    public class HelpModel : PageModel
    {
        public HelpTopic Topic { get; set; }

        public string Highlight { get; set; }

        public IEnumerable<HelpTopic> ShownTopics { get; private set;  }

        public void OnGet(string id, string from = null, [FromServices] IOptions<BrandConfig> brandconfig = null)
        {
            Highlight = from;

            if (!string.IsNullOrEmpty(id))
            {
                Topic = Topics.FirstOrDefault(x => x.Key == id);
                if (null == Topic)
                    Topic = new HelpTopic() { Title = "Sorry", Contents = new string[] { $"Can't find a help topic for <<{id}>>" } };
            }

            if (brandconfig?.Value.Exists ?? false)
                ShownTopics = Topics.Where(x => !x.ShowInDemoOnly);
            else
                ShownTopics = Topics;
        }

        public class HelpTopic
        {
            public string Key { get; set; }

            public string Title { get; set; }

            public string[] Contents { get; set; }

            public string[] Extended { get; set; } = new string[] { };

            public bool ExtendedIsList { get; set; }

            public string Button { get; set; }

            public string Href { get; set; }

            public bool ShowInDemoOnly { get; set; }
        }

        private IEnumerable<HelpTopic> Topics { get; } = new List<HelpTopic>()
        {
            new HelpTopic()
            {
                Key = "demo",

                Title = "Thanks for trying the Demo!",

                Contents = new string[]
                {
                    "This is a demo instance of YoFi running with realistic sample data. Feel free to have a look around and experiment with the software. If you want to run with your own data or modify the code for your own use, it's easy to <a href=\"https://github.com/jcoliz/yofi/blob/master/deploy/ARM-Template.md\">Deploy your own</a> instance in Azure.",
                    "You'll find a help topic for each page linked under the Actions menu. Or visit the <a href=\"/Help\">Help</a> page to see them all in one place."
                },

                Extended = new string[]
                {
                    "Once you're running your own instance, you can turn off the demo by configuring your site to include your own branding information, such as setting a site name and icon. See the <a href=\"https://github.com/jcoliz/yofi/blob/master/docs/Configuration.md\">Configuration</a> page for details."
                },

                Button = "Go",

                Href = "/Transactions",

                ShowInDemoOnly = true
            },
            new HelpTopic()
            {
                Key = "trans",

                Title = "Categorizing Transactions",

                Contents = new string[]
                {
                    "The Transactions page contains the complete list of transactions you've imported into the application. Your key task on this page is to assign categories to each transaction.",
                    "The easiest way is to set up <a href=\"/Help?from=payees#payees\">Payee Matching Rules</a>, to automatically assign categories when you import statements. You can also edit individual transactions, then enter a category in this dialog."
                },

                Button = "Transactions",

                Href = "/Transactions"
            },
            new HelpTopic()
            {
                Key = "payees",

                Title = "Payee Matching Rules",

                Contents = new string[]
                {
                    "You can set up payee matching rules so that new transactions are automatically assigned a category when imported. YoFi compares the payee of transactions against all the payee matching rules during import, looking for a substring match.",
                    "The easiest way to add a new rule is the 'Add Payee' button next to each item on the <a href=\"/Transactions/\">Transactions</a> page. ",
                },

                Extended = new string[]
                {
                    "E.g. a payee named 'Taco' will match transactions with payees 'Taco Tuesday' and 'Taco Wednesday'. For more advanced use, you can set a regular expression as the payee name. Indicate this by starting and ending the payee name with a slash. E.g. '/Taco.*?day/' will match 'Taco Tuesday' but not 'Taco Special'.",
                    "Clicking 'Add Payee' prompts you to give a category for this payee. This adds a new payee matching rule and assigns the category to the selected transaction. Along the way you can edit the payee name of the rule as well, perhaps shortening it to match potentially more transactions, as in the example above.",
                    "Of course, on the 'Payees' page, you can manually create new rules, edit and delete existing ones, or import and export your list of rules to an Excel spreadsheet."
                },

                Button = "Payees",

                Href = "/Payees"
            },
            new HelpTopic()
            {
                Key = "import",

                Title = "Transactions Import",

                Contents = new string[]
                {
                    "Using the Import page, you can import transactions from multiple Excel spreadsheets and/or OFX files. After importing, the new transactions will be held in a queue for you to review. Until you approve them, they are hidden from the Transactions page and excluded from your reports. ",
                },

                Extended = new string[]
                {
                    "To see an example of the expected format for an Excel spreadsheet, download an example from <a href=\"https://www.try-yofi.com/Transactions\">https://www.try-yofi.com/Transactions</a>. Note that most columns of the spreadsheet are optional."
                },

                Button = "Import",

                Href = "/Transactions/Import"
            },
            new HelpTopic()
            {
                Key = "budget",

                Title = "Creating a Budget",

                Contents = new string[]
                {
                    "To track your spending against a budget, you'll create individual Budget Line Items. Create one budget line item for each category you care about, setting the date to anything in the appropriate year (e.g. Jan 1st). The budget reports will interpret that as a budget for the whole year",
                    "The simplest way to enter this is to create the budget first in Excel, then import it on the Budget page. To see an example of how the spreadsheet should be laid out, download an example from <a href=\"https://www.try-yofi.com/BudgetTxs\">https://www.try-yofi.com/BudgetTxs</a>."
                },

                Extended = new string[]
                {

                    "If you're looking for more fine-grained control, you can create multiple budget line items for a single category at different dates through the year, for example once a month, each at different amounts if you like. Doing so will cause the category to show up in the 'Managed Budget' report, which tells you specifically how you're doing at the current point in time against these more-frequently-tracked budget lines.",
                },

                Button = "Budget",

                Href = "/BudgetTxs"
            },
            new HelpTopic()
            {
                Key = "xlsx",

                Title = "Spreadsheet Interoperability",

                Contents = new string[]
                {
                    "YoFi was born from spreadsheets, so interoperability is in its bones. This shows up in the following ways:"
                },

                Extended = new string[]
                {
                    "Import. Every data type can be imported from a spreadsheet into the app.",
                    "Export. Every data type can be exported out to a spreadsheet for separate processing.",
                    "Reports via API. Personally, I use YoFi as part of my overall workflow which is contained in Excel. You can easily set up a Power Query to fetch reports from the YoFi API. Every report available in the browser can be fetched using the API, with the same parameters. So it's easy to set up a master report page containing the precise level of detail you're looking for. Check out the <a href=\"https://github.com/jcoliz/yofi/blob/master/docs/SampleAPIConnection.xlsx\">example spreadsheet</a> which pulls data from www.try-yofi.com using the API.",
                    "Data via API. Every data type can be searched and download using the API, which can be imported into Excel using Power Query."
                },

                ExtendedIsList = true,
            }
        };
    }
}
