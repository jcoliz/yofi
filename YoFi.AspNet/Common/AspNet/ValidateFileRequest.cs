using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.AspNet
{
    public class ValidateFilesProvidedAttribute : Attribute, IAsyncActionFilter
    {
        private readonly bool _muiltiplefilesok;

        public ValidateFilesProvidedAttribute(bool multiplefilesok = true)
        {
            _muiltiplefilesok = multiplefilesok;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.ActionArguments.ContainsKey("files"))
            {
                var arg = context.ActionArguments["files"];

                if (arg == null)
                {
                    context.Result = new BadRequestResult();
                    return;
                }

                if (! (arg is List<IFormFile>))
                {
                    context.Result = new BadRequestResult();
                    return;
                }

                var files = arg as List<IFormFile>;

                if (!files.Any())
                {
                    context.Result = new BadRequestObjectResult("Must include at least one file");
                    return;
                }

                if (!_muiltiplefilesok && files.Skip(1).Any())
                {
                    context.Result = new BadRequestObjectResult("Must choose a only single file.");
                    return;
                }
            }
            else
            {
                context.Result = new BadRequestResult();
                return;
            }
            await next();
        }
    }
}
