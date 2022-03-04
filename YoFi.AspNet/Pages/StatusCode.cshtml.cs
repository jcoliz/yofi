using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YoFi.AspNet.Pages
{
    public class StatusCodeModel : PageModel
    {
        public int ErrorCode { get; set; }
        public string Reason { get; set; }

        public void OnGet(int e)
        {
            ErrorCode = e;

            Reason = Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(e);
        }

        public void OnPost(int e)
        {
            ErrorCode = e;

            Reason = Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(e);
        }
    }
}
