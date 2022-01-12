using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.DotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace YoFi.AspNet.Pages
{
    [AllowAnonymous]
    public class HomeModel : PageModel
    {
        public HomeModel(DemoConfig democonfig)
        {
            isDemo = democonfig.IsDemo;
        }

        public bool isDemo { get; private set; }

        public void OnGet()
        {
        }
    }
}
