using Common.NET;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.AspNet.Controllers
{
    public class ValidateStorageAvailableAttribute: ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpcontext = context.HttpContext;
            var service = httpcontext.RequestServices.GetService(typeof(IPlatformAzureStorage));

            if (service == null)
            {
                context.Result = new BadRequestObjectResult("Unable to process request. Azure Blob Storage is not configured for this application.") { StatusCode = 410 };
                return;
            }

            await next();
        }
    }
}
