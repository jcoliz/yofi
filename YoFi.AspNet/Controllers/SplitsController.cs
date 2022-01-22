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
    public class SplitsController : Controller, IController<Split>
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
        public async Task<IActionResult> DeleteConfirmed(int id, [FromServices] IRepository<Transaction> transactionRepository)
        {
            var split = await _repository.GetByIdAsync(id);
            var category = split.Category;
            var txid = split.TransactionID;

            await _repository.RemoveAsync(split);

            if (await transactionRepository.TestExistsByIdAsync(txid))
            {
                var tx = await transactionRepository.GetByIdAsync(txid);
                if ( !tx.HasSplits )
                {
                    tx.Category = category;
                    await transactionRepository.UpdateAsync(tx);
                }
            }
            // else if dones't exist, something bizarre happened, but we'll not take any action 

            return RedirectToAction("Edit", "Transactions", new { id = txid });
        }

        Task<IActionResult> IController<Split>.Download() =>
            throw new NotImplementedException();

        Task<IActionResult> IController<Split>.Upload(List<IFormFile> files) =>
            throw new NotImplementedException();

        Task<IActionResult> IController<Split>.Index() =>
            throw new NotImplementedException();

        Task<IActionResult> IController<Split>.Details(int? id) =>
            throw new NotImplementedException();

        Task<IActionResult> IController<Split>.Create(Split item) =>
            throw new NotImplementedException();

        Task<IActionResult> IController<Split>.Create() =>
            throw new NotImplementedException();

        Task<IActionResult> IController<Split>.DeleteConfirmed(int id) =>
            throw new NotImplementedException();
    }
}
