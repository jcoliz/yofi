using Ardalis.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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
    }
}
