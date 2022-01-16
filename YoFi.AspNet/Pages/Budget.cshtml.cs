using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Common.ChartJS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YoFi.AspNet.Pages.Helpers;
using YoFi.Core.Reports;

namespace YoFi.AspNet.Pages
{

    /// <summary>
    /// Budget summary page
    /// </summary>
    /// <remarks>
    /// This is just a shell to background load a budget vs actual report
    /// </remarks>
    [Authorize(Policy = "CanRead")]
    public class BudgetModel : PageModel
    {
        public int? Year { get; private set; }

        public void OnGet(int? y)
        {
            var sessionvars = new SessionVariables(HttpContext);

            if (y.HasValue)
                sessionvars.Year = y.Value;
            else
                y = sessionvars.Year;

            Year = y;
        }
    }
}
