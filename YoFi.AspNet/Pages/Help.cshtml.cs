using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YoFi.AspNet.Pages
{
    public class HelpModel : PageModel
    {
        public HelpTopic Topic { get; set; }

        public void OnGet(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                Topic = Topics.FirstOrDefault(x => x.Key == id);
                if (null == Topic)
                    Topic = new HelpTopic() { Title = "Sorry", Contents = new string[] { $"Can't find a help topic for <<{id}>>" } };
            }
        }

        public class HelpTopic
        {
            public string Key { get; set; }

            public string Title { get; set; }

            public string[] Contents { get; set; }

            public string[] Extended { get; set; }

            public string Button { get; set; }
            public string Href { get; set; }

        }

        public IEnumerable<HelpTopic> Topics { get; } = new List<HelpTopic>()
        {
            new HelpTopic()
            {
                Key = "payees",

                Title = "Payee Matching Rules",

                Contents = new string[]
                {
                    "You can set up payee matching rules so that new transactions are automatically assigned a category when imported. YoFi compares the payee of transactions against all the payee matching rules during import, looking for a substring match.",
                    "The easiest way to add a new rule is the 'Add Payee' button next to each item on the Transactions page. ",
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
                    "To see an example of the expected format for an Excel spreadsheet, download an example from https://www.try-yofi.com/Transactions. Note that most columns of the spreadsheet are optional."
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
                    "The simplest way to enter this is to create the budget first in Excel, then import it on the Budget page. To see an example of how the spreadsheet should be laid out, download an example from https://www.try-yofi.com/BudgetTxs."
                },

                Extended = new string[]
                {

                    "If you're looking for more fine-grained control, you can create multiple budget line items for a single category at different dates through the year, for example once a month, each at different amounts if you like. Doing so will cause the category to show up in the 'Managed Budget' report, which tells you specifically how you're doing at the current point in time against these more-frequently-tracked budget lines.",
                },

                Button = "Budget",

                Href = "/BudgetTxs"
            }
        };
    }
}
