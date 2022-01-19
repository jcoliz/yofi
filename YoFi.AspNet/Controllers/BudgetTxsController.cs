using Common.AspNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Repositories;
using YoFi.Core.Models;
using YoFi.Core.Importers;
using Ardalis.Filters;
using YoFi.Core;
using YoFi.Core.Repositories.Wire;

namespace YoFi.AspNet.Controllers
{
    [Authorize(Policy = "CanRead")]
    public class BudgetTxsController : Controller, IController<BudgetTx>
    {
        public static int PageSize { get; } = 25;

        private readonly IBudgetTxRepository _repository;

        public BudgetTxsController(IBudgetTxRepository repository)
        {
            _repository = repository;
        }

        // GET: BudgetTxs
        public async Task<IActionResult> Index(string q = null, string v = null, int? p = null)
        {

            //
            // Process QUERY (Q) parameters
            //

            ViewData["Query"] = q;

            //
            // Process VIEW (V) parameters
            //

            // "v=s" means show the selection checkbox. Used in bulk edit mode
            ViewData["ViewP"] = v;
            bool showSelected = v?.ToLowerInvariant().Contains("s") == true;
            ViewData["ShowSelected"] = showSelected;
            ViewData["ToggleSelected"] = showSelected ? null : "s";

            //
            // Run Query
            //

            var qresult = await _repository.GetByQueryAsync(new WireQueryParameters() { Query = q, Page = p });

            //
            // Process PAGE (P) parameters
            //

            var divider = new PageDivider();
            divider.BuildFromWirePageInfo(qresult.PageInfo);
            ViewData[nameof(PageDivider)] = divider;

            // Show the index
            return View(qresult.Items);
        }

        // GET: BudgetTxs/Details/5
        [ValidateBudgetTxExists]
        public async Task<IActionResult> Details(int? id)
        {
            return View(await _repository.GetByIdAsync(id));
        }

        // GET: BudgetTxs/Create
        public Task<IActionResult> Create()
        {
            return Task.FromResult(View() as IActionResult);
        }

        // POST: BudgetTxs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateModel]
        public async Task<IActionResult> Create([Bind("ID,Amount,Timestamp,Category,Memo,Frequency")] BudgetTx item)
        {
            await _repository.AddAsync(item);

            return RedirectToAction(nameof(Index));
        }

        // GET: BudgetTxs/Edit/5
        [ValidateBudgetTxExists]
        public async Task<IActionResult> Edit(int? id) => await Details(id);

        // POST: BudgetTxs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateModel]
        [ValidateBudgetTxExists]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Amount,Timestamp,Category,Memo,Frequency")] BudgetTx item)
        {
            try
            {
                if (id != item.ID)
                    throw new ArgumentException();

                await _repository.UpdateAsync(item);

                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
        }

        // GET: BudgetTxs/Delete/5
        public async Task<IActionResult> Delete(int? id) => await Details(id);

        // POST: BudgetTxs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateBudgetTxExists]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _repository.RemoveAsync(await _repository.GetByIdAsync(id));

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Upload(List<IFormFile> files, [FromServices] IImporter<BudgetTx> importer)
        {
            try
            {
                if (files == null || !files.Any())
                    throw new ApplicationException("Please choose a file to upload, first.");

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
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: BudgetTxs/Download
        public Task<IActionResult> Download()
        {
            var stream = _repository.AsSpreadsheet();

            var result = File(stream, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName: $"BudgetTx.xlsx");

            // Need to return a task to meet the IControllerBase interface
            return Task.FromResult(result as IActionResult);
        }

        Task<IActionResult> IController<BudgetTx>.Index() => Index();

        public Task<IActionResult> Upload(List<IFormFile> files) => Upload(files, new BaseImporter<BudgetTx>(_repository));
    }
}
