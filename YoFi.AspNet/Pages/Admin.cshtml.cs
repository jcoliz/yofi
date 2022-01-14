using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YoFi.Core;

namespace YoFi.AspNet.Pages
{
    [Authorize(Roles = "Admin")]
    public class AdminModel : PageModel
    {
        public bool HasData { get; set; }
        public int NumTransactions { get; set; }
        public int NumBudgetTxs { get; set; }
        public int NumPayees { get; set; }

        private readonly IDataContext _context;

        public AdminModel(IDataContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync()
        {
            // TODO: CountAsync()
            NumTransactions = _context.Transactions.Count();
            NumBudgetTxs = _context.BudgetTxs.Count();
            NumPayees = _context.Payees.Count();

            HasData = NumTransactions > 0 || NumBudgetTxs > 0 || NumPayees > 0;
        }
    }
}
