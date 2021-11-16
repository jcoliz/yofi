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

namespace YoFi.AspNet.Controllers
{
    [Authorize(Policy = "CanRead")]
    public class PayeesController : Controller, IController<Payee>
    {
        public static int PageSize { get; } = 25;

        private readonly IPayeeRepository _repository;
        private readonly IAsyncQueryExecution _queryExecution;

        public PayeesController(IPayeeRepository repository, IAsyncQueryExecution queryExecution)
        {
            _repository = repository;
            _queryExecution = queryExecution;
        }

        // GET: Payees
        public async Task<IActionResult> Index(string q = null, string v = null, int? p = null)
        {
            //
            // Process QUERY (Q) parameters
            //

            var result = _repository.ForQuery(q);

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
            // Process PAGE (P) parameters
            //

            var divider = new PageDivider() { PageSize = PageSize, ViewParameters = new PageDivider.DefaultViewParameters() { QueryParameter = q, ViewParameter = v } };
            result = await divider.ItemsForPage(result, p);
            ViewData[nameof(PageDivider)] = divider;

            return View(await _queryExecution.ToListNoTrackingAsync(result));
        }

        // GET: Payees/Details/5
        [ValidatePayeeExists]
        public async Task<IActionResult> Details(int? id)
        {
            return View(await _repository.GetByIdAsync(id));
        }

        // GET: Payees/Create
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
                // NewFromTransaction will throw this if txid no exists
                // Once I have a transactions repository, I can use ValidateTransactionExists here
                return NotFound();
            }
        }
        // GET: Payees/CreateModel/{id}
        public async Task<IActionResult> CreateModal(int id)
        {
            try
            {
                if (id <= 0)
                    throw new ArgumentException("Transaction ID needed", nameof(id));

                ViewData["TXID"] = id;

                return PartialView("CreatePartial", await _repository.NewFromTransactionAsync(id));
            }
            catch (ArgumentException ex)
            {
                return BadRequest($"Bad Request: {ex.Message}");
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }

        // POST: Payees/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateModel]
        public async Task<IActionResult> Create([Bind("ID,Name,Category,SubCategory")] Payee payee)
        {
            await _repository.AddAsync(payee);

            return RedirectToAction(nameof(Index));
        }

        // GET: Payees/Edit/5
        [ValidatePayeeExists]
        public async Task<IActionResult> Edit(int? id) => await Details(id);

        // GET: Payees/EditModal/5
        [ValidatePayeeExists]
        public async Task<IActionResult> EditModal(int? id)
        {
            return PartialView("EditPartial", await _repository.GetByIdAsync(id));
        }


        // POST: Payees/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateModel]
        [ValidatePayeeExists]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,Category,SubCategory")] Payee item)
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

        // POST: Payees/BulkEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> BulkEdit(string Category)
        {
            await _repository.BulkEditAsync(Category);

            return RedirectToAction(nameof(Index));
        }

        // POST: Payees/BulkDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> BulkDelete()
        {
            await _repository.BulkDeleteAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Payees/Delete/5
        [ValidatePayeeExists]
        public async Task<IActionResult> Delete(int? id) => await Details(id);

        // POST: Payees/Delete/5
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
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Upload(List<IFormFile> files, [FromServices] IImporter<Payee> importer)
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

        // GET: Payees/Download
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
