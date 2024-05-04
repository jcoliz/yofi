using Ardalis.Filters;
using Common.AspNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.AspNet.Controllers
{
    [Authorize(Policy = "CanRead")]
    public class SplitsController : Controller
    {
        private readonly IRepository<Split> _repository;

        public SplitsController(IRepository<Split> repository)
        {
            _repository = repository;
        }

        [ValidateSplitExists]
        public async Task<IActionResult> Edit(int? id)
        {

            var split = await _repository.GetByIdAsync(id);
            return View(split);
        }

        [HttpPost]
        [Authorize(Policy = "CanWrite")]
        [ValidateAntiForgeryToken]
        [ValidateModel]
        [ValidateSplitExists]
        public async Task<IActionResult> Edit(int id, [Bind("ID,TransactionID,Amount,Category,Memo")] Split split)
        {
            await _repository.UpdateAsync(split);
            return RedirectToAction("Edit","Transactions", new { id = split.TransactionID });
        }

        public async Task<IActionResult> Delete(int? id) => await Edit(id);

        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = "CanWrite")]
        [ValidateAntiForgeryToken]
        [ValidateSplitExists]
        public async Task<IActionResult> DeleteConfirmed(int id, [FromServices] ITransactionRepository transactionRepository)
        {
            var txid = await transactionRepository.RemoveSplitAsync(id);
            return RedirectToAction("Edit", "Transactions", new { id = txid });
        }
    }
}
