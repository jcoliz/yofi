using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.AspNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YoFi.Core.Models;

namespace YoFi.AspNet.Pages
{
    public class ImportModel : PageModel
    {
        public PageDivider Divider { get; private set; } = new PageDivider();

        public IEnumerable<Transaction> Transactions => new List<Transaction>() { new Transaction() };

        public void OnGet()
        {
        }
    }
}
