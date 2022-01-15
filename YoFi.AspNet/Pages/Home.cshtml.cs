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
using YoFi.Core;

namespace YoFi.AspNet.Pages
{
    [AllowAnonymous]
    public class HomeModel : PageModel
    {
        /// <summary>
        /// Whether the next step the user takes should be to the admin page
        /// </summary>
        public bool IsRouteToAdmin { get; set; }

        public HomeModel(DemoConfig democonfig)
        {
            isDemo = democonfig.IsEnabled;
        }

        public bool isDemo { get; private set; }

        public void OnGet([FromServices] IDataContext context)
        {
            // Task 1270: Tie Admin page into Home page flow
            //
            // The "get started" button should go to the admin page IF the database is empty AND (either the current user is not logged in OR current user is admin)

            var isempty = !context.Transactions.Any();
            var loggedin = User.Identity.IsAuthenticated;
            var isadmin = User.IsInRole("Admin");

            IsRouteToAdmin = isempty && (!loggedin || isadmin);
        }
    }
}
