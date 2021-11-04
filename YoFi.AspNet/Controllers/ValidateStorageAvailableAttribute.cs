using Common.NET;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
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
            var storage = httpcontext.RequestServices.GetService(typeof(IPlatformAzureStorage));
            var config = httpcontext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;

            if (storage == null)
            {
                context.Result = new BadRequestObjectResult("Unable to process request. Azure Blob Storage is not configured for this application.") { StatusCode = 410 };
                return;
            }

            if (config["Storage:BlobContainerName"] == null)
            {
                context.Result = new BadRequestObjectResult("Unable to process request. No Azure Blob Storage container is not configured for this application.") { StatusCode = 410 };
                return;
            }

            await next();
        }
    }
}
