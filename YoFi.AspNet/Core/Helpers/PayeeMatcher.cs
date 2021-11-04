using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.Core.Helpers
{
    /// <summary>
    /// Set categories for imported transactions based on payee matching rules
    /// </summary>
    public class PayeeMatcher
    {
        List<Payee> payees;
        IEnumerable<Payee> regexpayees;

        readonly IDataContext _mycontext;

        public PayeeMatcher(IDataContext context)
        {
            _mycontext = context;
        }

        public async Task LoadAsync()
        {
            // Load all payees into memory. This is an optimization. Rather than run a separate payee query for every 
            // transaction, we'll pull it all into memory. This assumes the # of payees is not out of control.

            payees = await _mycontext.Payees.ToListAsync();
            regexpayees = payees.Where(x => x.Name.StartsWith("/") && x.Name.EndsWith("/"));
        }

        public async Task<bool> SetCategoryBasedOnMatchingPayeeAsync(Transaction item)
        {
            var result = false;

            if (string.IsNullOrEmpty(item.Category))
            {
                Payee payee = null;
                string strippedpayee = item.StrippedPayee;

                if (payees == null)
                    await LoadAsync();

                // Product Backlog Item 871: Match payee on regex, optionally
                foreach (var regexpayee in regexpayees)
                {
                    var regex = new Regex(regexpayee.Name[1..^2]);
                    if (regex.Match(strippedpayee).Success)
                    {
                        payee = regexpayee;
                        break;
                    }
                }

                if (null == payee)
                {
                    //TODO: FirstOrDefaultAsync()
                    payee = payees.FirstOrDefault(x => strippedpayee.Contains(x.Name));
                }

                if (null != payee)
                {
                    item.Category = payee.Category;
                    result = true;
                }
            }

            return result;
        }
    };
}
