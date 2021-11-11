using Ardalis.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.AspNet.Controllers
{
    [Produces("application/json")]
    [Route("ajax/tx")]
    public class AjaxTransactionController: Controller
    {
        private readonly ITransactionRepository _repository;

        public AjaxTransactionController(ITransactionRepository repository)
        {
            _repository = repository;
        }

        [HttpPost("select/{id}")]
        [ValidateAntiForgeryToken]
        [ValidateTransactionExists]
        public async Task<IActionResult> Select(int id, bool value)
        {
            var item = await _repository.GetByIdAsync(id);
            item.Selected = value;
            await _repository.UpdateAsync(item);

            return new OkResult();
        }

        [HttpPost("hide/{id}")]
        [ValidateAntiForgeryToken]
        [ValidateTransactionExists]
        public async Task<IActionResult> Hide(int id, bool value)
        {
            var item = await _repository.GetByIdAsync(id);
            item.Hidden = value;
            await _repository.UpdateAsync(item);

            return new OkResult();
        }

        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateModel]
        [ValidateTransactionExists]
        public async Task<IActionResult> Edit(int id, [Bind("Memo,Payee,Category")] Transaction edited)
        {
            var item = await _repository.GetByIdAsync(id);
            item.Memo = edited.Memo;
            item.Payee = edited.Payee;
            item.Category = edited.Category;
            await _repository.UpdateAsync(item);
            return new ObjectResult(item);
        }

        [HttpPost("applypayee/{id}")]
        [Authorize(Policy = "CanWrite")]
        [ValidateTransactionExists]
        public async Task<IActionResult> ApplyPayee(int id, [FromServices] IPayeeRepository payeeRepository)
        {
            var item = await _repository.GetByIdAsync(id);

            var category = await payeeRepository.GetCategoryMatchingPayeeAsync(item.StrippedPayee);
            if (category != null)
            {
                item.Category = category;
                await _repository.UpdateAsync(item);
                return new OkObjectResult(category);
            }
            else
                return new NotFoundObjectResult($"Payee {item.StrippedPayee} not found");
        }

        [HttpGet("cat-ac")]
        [Authorize(Policy = "CanRead")]
        public IActionResult CategoryAutocomplete(string q)
        {
            const int numresults = 10;

            // Look for top N recent categories in transactions, first.
            var txd = _repository.All.Where(x => x.Timestamp > DateTime.Now.AddMonths(-12) && x.Category.Contains(q)).GroupBy(x => x.Category).Select(g => new { Key = g.Key, Value = g.Count() }).OrderByDescending(x => x.Value).Take(numresults);

            // There are also some categories in splits. Get the top N there too.
            var spd = _repository.Splits.Where(x => x.Transaction.Timestamp > DateTime.Now.AddMonths(-12) && x.Category.Contains(q)).GroupBy(x => x.Category).Select(g => new { Key = g.Key, Value = g.Count() }).OrderByDescending(x => x.Value).Take(numresults);

            // Merge the results

            // https://stackoverflow.com/questions/2812545/how-do-i-sum-values-from-two-dictionaries-in-c
            // TODO: ToListAsync();
            var result = txd.Concat(spd).GroupBy(x => x.Key).Select(x => new { Key = x.Key, Value = x.Sum(g => g.Value) }).OrderByDescending(x => x.Value).Take(numresults).Select(x => x.Key).ToList();

            return new OkObjectResult(result);

            /* Just want to say how impressed I am with myself for getting this query to entirely run on server side :D
             * 
                  SELECT TOP(@__p_1) [t3].[Key]
                  FROM (
                      SELECT [t0].[Category] AS [Key], [t0].[c] AS [Value]
                      FROM (
                          SELECT TOP(@__p_1) [t].[Category], COUNT(*) AS [c]
                          FROM [Transactions] AS [t]
                          WHERE ([t].[Timestamp] > DATEADD(month, CAST(-12 AS int), GETDATE())) AND ((@__q_0 = N'') OR (CHARINDEX(@__q_0, [t].[Category]) > 0))
                          GROUP BY [t].[Category]
                          ORDER BY COUNT(*) DESC
                      ) AS [t0]
                      UNION ALL
                      SELECT [t2].[Category] AS [Key], [t2].[c] AS [Value]
                      FROM (
                          SELECT TOP(@__p_1) [s].[Category], COUNT(*) AS [c]
                          FROM [Split] AS [s]
                          INNER JOIN [Transactions] AS [t1] ON [s].[TransactionID] = [t1].[ID]
                          WHERE ([t1].[Timestamp] > DATEADD(month, CAST(-12 AS int), GETDATE())) AND ((@__q_0 = N'') OR (CHARINDEX(@__q_0, [s].[Category]) > 0))
                          GROUP BY [s].[Category]
                          ORDER BY COUNT(*) DESC
                      ) AS [t2]
                  ) AS [t3]
                  GROUP BY [t3].[Key]
                  ORDER BY SUM([t3].[Value]) DESC
            */
        }

    }
}
