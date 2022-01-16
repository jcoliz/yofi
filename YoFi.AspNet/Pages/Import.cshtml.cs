using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.AspNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YoFi.Core.Models;

namespace YoFi.AspNet.Pages
{
    public class ImportModel : PageModel
    {
        public PageDivider Divider { get; private set; } = new PageDivider();

        public IEnumerable<Transaction> Transactions => Enumerable.Empty<Transaction>(); //new List<Transaction>() { new Transaction() };

        public void OnGet()
        {
        }

        public void OnPostGo(string command)
        {

        }
        public void OnPostUpload(List<IFormFile> files)
        {

        }
    }
}
