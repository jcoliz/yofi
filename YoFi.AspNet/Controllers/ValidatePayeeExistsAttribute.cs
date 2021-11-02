using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.AspNet.Controllers
{

    public class ValidatePayeeExistsAttribute : TypeFilterAttribute
    {
        public ValidatePayeeExistsAttribute() : base(typeof
          (ValidateAuthorExistsFilterImpl<Payee>))
        {
        }
    }

    public class ValidateBudgetTxExistsAttribute : TypeFilterAttribute
    {
        public ValidateBudgetTxExistsAttribute() : base(typeof
          (ValidateAuthorExistsFilterImpl<BudgetTx>))
        {
        }
    }

    internal class ValidateAuthorExistsFilterImpl<T> : IAsyncActionFilter where T : class, IModelItem<T>
    {
        private readonly IRepository<T> _datacontext;
        public ValidateAuthorExistsFilterImpl(IRepository<T> repository)
        {
            _datacontext = repository;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context,
          ActionExecutionDelegate next)
        {
            if (context.ActionArguments.ContainsKey("id"))
            {
                var id = context.ActionArguments["id"] as int?;
                if (id.HasValue)
                {
                    if (!await _datacontext.TestExistsByIdAsync(id.Value))
                    {
                        context.Result = new NotFoundResult();
                        return;
                    }
                }
            }
            await next();
        }
    }
}
