using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YoFi.Core.Reports;

namespace YoFi.AspNet.Pages
{
    public class BudgetModel : PageModel
    {
        private readonly IReportEngine _reportengine;

        public Report Report { get; set; }

        public string ChartJson { get; set; }

        public BudgetModel(IReportEngine reportengine)
        {
            _reportengine = reportengine;
        }

        public void OnGet()
        {
            Report = _reportengine.Build(new ReportParameters() { id = "expenses-v-budget" });
        }
    }
}
