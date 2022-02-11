using Ardalis.Filters;
using Common.AspNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core;
using YoFi.Core.Importers;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Core.Repositories.Wire;

namespace YoFi.AspNet.Controllers
{
    [Authorize(Policy = "CanRead")]
    public class PayeesController : Controller, IController<Payee>
    {
        private readonly IPayeeRepository _repository;

        public PayeesController(IPayeeRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index(string q = null, string v = null, int? p = null)
        {
            //
            // Process VIEW (V) parameters
            //

            // "v=s" means show the selection checkbox. Used in bulk edit mode
            bool showSelected = v?.ToLowerInvariant().Contains("s") == true;
            ViewData["ShowSelected"] = showSelected;
            ViewData["ToggleSelected"] = showSelected ? null : "s";

            //
            // Run Query
            //

            var qresult = await _repository.GetByQueryAsync(new WireQueryParameters() { Query = q, Page = p, View = v });
            return View(qresult);
        }

        [ValidatePayeeExists]
        public async Task<IActionResult> Details(int? id)
        {
            return View(await _repository.GetByIdAsync(id));
        }

        public async Task<IActionResult> Create(int? txid)
        {
            try
            {
                // Create with no txid is valid. It means user is creating one from scratch
                if (!txid.HasValue)
                    return View();

                return View(await _repository.NewFromTransactionAsync(txid.Value));
            }
            catch (InvalidOperationException)
            {
                // Note that I can't use [ValidateTransactionExists] because calling this with
                // null is valid.

                return NotFound();
            }
        }

        [ValidateTransactionExists]
        public async Task<IActionResult> CreateModal(int id)
        {
            ViewData["TXID"] = id;

            return PartialView("CreatePartial", await _repository.NewFromTransactionAsync(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateModel]
        public async Task<IActionResult> Create([Bind("ID,Name,Category")] Payee payee)
        {
            await _repository.AddAsync(payee);

            return RedirectToAction(nameof(Index));
        }

        [ValidatePayeeExists]
        public async Task<IActionResult> Edit(int? id) => await Details(id);

        [ValidatePayeeExists]
        public async Task<IActionResult> EditModal(int? id)
        {
            return PartialView("EditPartial", await _repository.GetByIdAsync(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateModel]
        [ValidatePayeeExists]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,Category")] Payee item)
        {
            await _repository.UpdateAsync(item);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> BulkEdit(string Category)
        {
            await _repository.BulkEditAsync(Category);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> BulkDelete()
        {
            await _repository.BulkDeleteAsync();

            return RedirectToAction(nameof(Index));
        }

        [ValidatePayeeExists]
        public async Task<IActionResult> Delete(int? id) => await Details(id);

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidatePayeeExists]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _repository.RemoveAsync(await _repository.GetByIdAsync(id));

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateFilesProvided(multiplefilesok: true)]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Upload(List<IFormFile> files, [FromServices] IImporter<Payee> importer)
        {
            foreach (var file in files)
            {
                if (file.FileName.ToLower().EndsWith(".xlsx"))
                {
                    using var stream = file.OpenReadStream();
                    importer.QueueImportFromXlsx(stream);
                }
            }

            var imported = await importer.ProcessImportAsync();

            return View(imported);
        }

        [ActionName("Download")]
        public Task<IActionResult> Download()
        {
            var stream = _repository.AsSpreadsheet();

            IActionResult result = File(stream, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName: $"Payee.xlsx");

            // Need to return a task to meet the IControllerBase interface
            return Task.FromResult(result);
        }
        Task<IActionResult> IController<Payee>.Create() => Create((int?)null);

        Task<IActionResult> IController<Payee>.Index() => Index();

        Task<IActionResult> IController<Payee>.Upload(List<IFormFile> files) => Upload(files, new BaseImporter<Payee>(_repository));
    }
}
