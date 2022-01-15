using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.DotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using YoFi.Core;

namespace YoFi.AspNet.Pages
{
    [Authorize(Roles = "Admin")]
    public class AdminModel : PageModel
    {
        public bool HasSomeData { get; set; }
        public bool HasAllData { get; set; }
        public int NumTransactions { get; set; }
        public bool MoreTransactionsReady { get; set; }
        public int NumBudgetTxs { get; set; }
        public int NumPayees { get; set; }
        public PageConfig Config { get; private set; }
        public IClock Clock { get; private set; }

        private readonly IDataContext _context;


        public class PageConfig
        {
            public const string Section = "Admin";

            public bool NoDelete { get; set; }
        }

        public AdminModel(IDataContext context, IOptions<PageConfig> config, IClock clock)
        {
            _context = context;
            Clock = clock;
            Config = config.Value;
        }

        public async Task OnGetAsync()
        {
            // TODO: CountAsync()
            NumTransactions = _context.Transactions.Count();
            NumBudgetTxs = _context.BudgetTxs.Count();
            NumPayees = _context.Payees.Count();

            if (NumTransactions > 0)
            {
                var latest = _context.Transactions.OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp).First();
                MoreTransactionsReady = Clock.Now - latest > TimeSpan.FromDays(7);
            }
            else
                MoreTransactionsReady = true;

            HasSomeData = NumTransactions > 0 || NumBudgetTxs > 0 || NumPayees > 0;
            HasAllData = NumTransactions > 0 && NumBudgetTxs > 0 && NumPayees > 0 && ! MoreTransactionsReady;
        }
    }
}
