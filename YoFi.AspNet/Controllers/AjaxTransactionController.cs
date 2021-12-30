using Ardalis.Filters;
using Common.AspNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.AspNet.Controllers
{
    [Route("ajax/tx")]
    [Produces("application/json")]
    [SkipStatusCodePages]
    public class AjaxTransactionController: Controller
    {
        private readonly ITransactionRepository _repository;

        public AjaxTransactionController(ITransactionRepository repository)
        {
            _repository = repository;
        }

        [HttpPost("select/{id}")]
        [Authorize(Policy = "CanWrite")]
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
        [Authorize(Policy = "CanWrite")]
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
        [Authorize(Policy = "CanWrite")]
        [ValidateAntiForgeryToken]
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
                var result = category;

                // Consider custom split rules based on matched category
                var customsplits = _repository.CalculateCustomSplitRules(item, category);
                if (customsplits.Any())
                {
                    item.Splits = customsplits.ToList();
                    result = "SPLIT"; // This is what we display in the UI to indicate a transaction has a split
                }
                else
                    item.Category = category;

                await _repository.UpdateAsync(item);
                return new OkObjectResult(result);
            }
            else
                return new NotFoundObjectResult($"Payee {item.StrippedPayee} not found");
        }

        [HttpPost("uprcpt/{id}")]
        [Authorize(Policy = "CanWrite")]
        [ValidateAntiForgeryToken]
        [ValidateTransactionExists]
        [ValidateStorageAvailable]
        public async Task<IActionResult> UpReceipt(int id, IFormFile file)
        {

            try
            {
                var item = await _repository.GetByIdAsync(id);

                if (!string.IsNullOrEmpty(item.ReceiptUrl))
                    throw new ApplicationException($"This transaction already has a receipt. Delete the current receipt before uploading a new one.");


                using var stream = file.OpenReadStream();
                await _repository.UploadReceiptAsync(item, stream, file.ContentType);

                return new OkResult();

            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [HttpGet("cat-ac")]
        [Authorize(Policy = "CanRead")]
        public async Task<IActionResult> CategoryAutocompleteAsync(string q)
        {
            var result = await _repository.CategoryAutocompleteAsync(q);
            return new OkObjectResult(result);
        }
    }
}
