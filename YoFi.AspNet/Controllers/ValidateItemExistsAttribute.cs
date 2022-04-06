using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.AspNet.Controllers
{
    public class ValidateTransactionExistsAttribute : TypeFilterAttribute
    {
        public ValidateTransactionExistsAttribute() : base(typeof
          (ValidateItemExists<Transaction>))
        {
        }
    }

    public class ValidatePayeeExistsAttribute : TypeFilterAttribute
    {
        public ValidatePayeeExistsAttribute() : base(typeof
          (ValidateItemExists<Payee>))
        {
        }
    }

    public class ValidateBudgetTxExistsAttribute : TypeFilterAttribute
    {
        public ValidateBudgetTxExistsAttribute() : base(typeof
          (ValidateItemExists<BudgetTx>))
        {
        }
    }

    public class ValidateSplitExistsAttribute : TypeFilterAttribute
    {
        public ValidateSplitExistsAttribute() : base(typeof
          (ValidateItemExists<Split>))
        {
        }
    }

    public class ValidateReceiptExistsAttribute : TypeFilterAttribute
    {
        public ValidateReceiptExistsAttribute() : base(typeof
          (ValidateItemExists<Receipt>))
        {
        }
    }

    // https://docs.microsoft.com/en-us/archive/msdn-magazine/2016/august/asp-net-core-real-world-asp-net-core-mvc-filters

    internal class ValidateItemExists<T> : IAsyncActionFilter where T : class, IID
    {
        private readonly IDataProvider _context;
        public ValidateItemExists(IDataProvider context)
        {
            _context = context;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context,
          ActionExecutionDelegate next)
        {
            if (context.ActionArguments.ContainsKey("id"))
            {
                var id = context.ActionArguments["id"] as int?;
                if (id.HasValue)
                {
                    var query = _context.Get<T>().Where(x => x.ID == id.Value);
                    var exists = await _context.AnyAsync(query);

                    if (!exists)
                    {
                        context.Result = new NotFoundResult();
                        return;
                    }
                }
                else
                {
                    context.Result = new BadRequestResult();
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
