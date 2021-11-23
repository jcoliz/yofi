using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using YoFi.Core;

namespace Common.AspNet
{
    public class ValidateStorageAvailableAttribute: ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpcontext = context.HttpContext;
            var storage = httpcontext.RequestServices.GetService(typeof(IStorageService)) as IStorageService;

            if (storage == null)
            {
                context.Result = new BadRequestObjectResult("Unable to process request. Azure Blob Storage is not configured for this application. [E1]") { StatusCode = 410 };
                return;
            }

            if (string.IsNullOrEmpty(storage.ContainerName))
            {
                context.Result = new BadRequestObjectResult("Unable to process request. No Azure Blob Storage container is configured for this application. [E2]") { StatusCode = 410 };
                return;
            }

            await next();
        }
    }
}
