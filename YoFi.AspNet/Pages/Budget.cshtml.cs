using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Common.ChartJS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YoFi.Core.Reports;

namespace YoFi.AspNet.Pages
{
    /// <summary>
    /// Budget summary page
    /// </summary>
    /// <remarks>
    /// This is just a shell to background load a budget vs actual report
    /// </remarks>
    public class BudgetModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
