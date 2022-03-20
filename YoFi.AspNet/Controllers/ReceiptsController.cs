using Common.AspNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Repositories;

namespace YoFi.AspNet.Controllers
{
    [Authorize("CanRead")]
    public class ReceiptsController: Controller
    {
        private readonly IReceiptRepository _repository;
        private readonly ITransactionRepository _txrepository;

        public ReceiptsController(IReceiptRepository repository, ITransactionRepository txrepository)
        {
            _repository = repository;
            _txrepository = txrepository;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var qresult = await _repository.GetAllAsync();
                return View(qresult);

            }
            catch
            {
                throw;
            }
        }

        //TODO: [ValidateTransactionExists("txid")]
        public async Task<IActionResult> Pick(int txid)
        {
            try
            {
                Console.WriteLine($"Pick: {txid}");

                var tx = await _txrepository.GetByIdAsync(txid);
                var qresult = await _repository.GetMatchingAsync(tx);

                ViewData["txid"] = txid;

                Console.WriteLine($"Found: {qresult.Count()}");

                return View(qresult);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return BadRequest(ex.Message);
                //throw;
            }
        }

        [ValidateReceiptExists]
        public async Task<IActionResult> Details(int id)
        {
            var receipt = await _repository.GetByIdAsync(id);
            return PartialView(receipt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateReceiptExists]
        public async Task<IActionResult> Action(int id, string command, int? txid)
        {
            switch (command)
            {
                case "Delete":
                    return await Delete(id);
                case "Accept":
                    return await Accept(id, txid.Value, null);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateReceiptExists]
        public async Task<IActionResult> Delete(int id)
        {
            await _repository.DeleteAsync(await _repository.GetByIdAsync(id));
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateReceiptExists]
        //TODO: [ValidateTransactionExists("txid")]
        public async Task<IActionResult> Accept(int id, int txid, string next)
        {
            var receipt = await _repository.GetByIdAsync(id);
            var tx = await _txrepository.GetByIdAsync(txid);

            try
            {
                await _repository.AssignReceipt(receipt, tx);
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch
            {
                throw;
            }

            if ("edittx" == next)
                return RedirectToAction(nameof(TransactionsController.Edit),"Transactions", new { id = txid });
            else
                return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> AcceptAll()
        {
            await _repository.AssignAll();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateFilesProvided(multiplefilesok: true)]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            foreach (var file in files)
            {
                using var stream = file.OpenReadStream();
                await _repository.UploadReceiptAsync(file.FileName, stream, file.ContentType);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
