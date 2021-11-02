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

namespace YoFi.AspNet.Controllers
{
    [Authorize(Policy = "CanRead")]
    public class BudgetTxsController : Controller, IController<BudgetTx>
    {
        public static int PageSize { get; } = 25;

        private readonly IRepository<BudgetTx> _repository;

        public BudgetTxsController(IRepository<BudgetTx> repository)
        {
            _repository = repository;
        }

        // GET: BudgetTxs
        public async Task<IActionResult> Index(string q = null, int? p = null)
        {

            //
            // Process QUERY (Q) parameters
            //

            ViewData["Query"] = q;

            var result = _repository.ForQuery(q);

            //
            // Process PAGE (P) parameters
            //

            var divider = new PageDivider() { PageSize = PageSize };
            result = await divider.ItemsForPage(result, p);
            ViewData[nameof(PageDivider)] = divider;

            // Show the index
            // TODO: Would like to do ToListAsync
            return View(result.ToList());
        }

        // GET: BudgetTxs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            try
            {
                return View(await _repository.GetByIdAsync(id));
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
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
        public async Task<IActionResult> Create([Bind("ID,Amount,Timestamp,Category")] BudgetTx item)
        {
            await _repository.AddAsync(item);

            return RedirectToAction(nameof(Index));
        }

        // GET: BudgetTxs/Edit/5
        public async Task<IActionResult> Edit(int? id) => await Details(id);

        // POST: BudgetTxs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateModel]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Amount,Timestamp,Category")] BudgetTx item)
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
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var item = await _repository.GetByIdAsync(id);

                await _repository.RemoveAsync(item);

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Upload(List<IFormFile> files)
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
                        _repository.QueueImportFromXlsx(stream);
                    }
                }

                var imported = await _repository.ProcessImportAsync();

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
    }
}
