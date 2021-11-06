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
            var key = string.IsNullOrEmpty(id) ? "(blank)" : id;

            if (Topics.ContainsKey(key))
                Topic = Topics[key];
            else
                Topic = new HelpTopic() { Title = "Sorry", Contents = new string[] { $"Can't find a help topic for <<{key}>>" } };
        }

        public class HelpTopic
        {
            public string Title { get; set; }

            public string[] Contents { get; set; }

            public string[] Full { get; set; }

        }

        private Dictionary<string, HelpTopic> Topics = new Dictionary<string, HelpTopic>()
        {
            { 
                "payees", new HelpTopic()
                {
                    Title = "Payee Matching Rules",

                    Contents = new string[]
                    {
                        "You can set up payee matching rules so that new transactions are automatically assigned a category when imported. YoFi compares the payee of transactions against all the payee matching rules during import, looking for a substring match.",
                        "The easiest way to add a new rule is the 'Add Payee' button next to each item on the Transactions page. ",
                    },

                    Full = new string[]
                    {
                        "You can set up payee matching rules so that new transactions are automatically assigned a category when imported. YoFi compares the payee of transactions against all the payee matching rules during import, looking for a substring match. E.g. a payee named 'Taco' will match transactions with payees 'Taco Tuesday' and 'Taco Wednesday'. For more advanced use, you can set a regular expression as the payee name. Indicate this by starting and ending the payee name with a slash. E.g. '/Taco.*?day/' will match 'Taco Tuesday' but not 'Taco Special'.",
                        "The easiest way to add a new rule is the 'Add Payee' button next to every transaction. Clicking it prompts you to give a category for this payee. This adds a new payee matching rule and assigns the category to the selected transaction. Along the way you can edit the payee name of the rule as well, perhaps shortening it to match potentially more transactions, as in the example above.",
                        "Of course, on the 'Payees' page, you can manually create new rules, edit and delete existing ones, or import and export your list of rules to an Excel spreadsheet."
                    }
                }
            }

        };
    }
}
