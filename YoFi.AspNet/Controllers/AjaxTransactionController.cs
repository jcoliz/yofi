using Ardalis.Filters;
using Common.AspNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SendGrid.Helpers.Errors.Model;
using System;
using System.Collections.Generic;
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
            await _repository.SetSelectedAsync(id, value);
            return new OkResult();
        }

        [HttpPost("hide/{id}")]
        [Authorize(Policy = "CanWrite")]
        [ValidateAntiForgeryToken]
        [ValidateTransactionExists]
        public async Task<IActionResult> Hide(int id, bool value)
        {
            await _repository.SetHiddenAsync(id, value);
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
        [ValidateAntiForgeryToken]
        [ValidateTransactionExists]
        public async Task<IActionResult> ApplyPayee(int id, [FromServices] IPayeeRepository payeeRepository)
        {
            try
            {
                var result = await _repository.ApplyPayeeAsync(id);
                return new OkObjectResult(result);
            }
            catch (KeyNotFoundException ex) 
            {
                return new NotFoundObjectResult(ex.Message);
            }
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
                using var stream = file.OpenReadStream();
                await _repository.UploadReceiptAsync(id, stream, file.ContentType);

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
