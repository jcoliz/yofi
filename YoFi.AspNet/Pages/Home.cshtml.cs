using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace Yofi.AspNet.Pages
{
    public class HomeModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public HomeModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool isDemo => ! _configuration.GetSection("Brand").Exists();

        public void OnGet()
        {
        }
    }
}
