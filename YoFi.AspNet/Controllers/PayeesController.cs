using Common.AspNet;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Repositories;
using YoFi.AspNet.Data;
using YoFi.Core.Models;
using YoFi.Core.Importers;

namespace YoFi.AspNet.Controllers
{
    [Authorize(Policy = "CanRead")]
    public class PayeesController : Controller, IController<Payee>
    {
        public static int PageSize { get; } = 25;

        private readonly IPayeeRepository _repository;

        public PayeesController(IPayeeRepository repository)
        {
            _repository = repository;
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

            var divider = new PageDivider() { PageSize = PageSize };
            result = await divider.ItemsForPage(result, p);
            ViewData[nameof(PageDivider)] = divider;

            // TODO: ToListAsync()
            return View(result.ToList());
        }

        Task<IActionResult> IController<Payee>.Index() => Index();

        // GET: Payees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            try
            {
                var item = await _repository.GetByIdAsync(id);
                return View(item);
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // GET: Payees/Create
        public async Task<IActionResult> Create(int? txid)
        {
            try
            {
                // Create with no txid is valid. It means user is creating one from scratch
                if (!txid.HasValue)
                    return View();

                return View(await _repository.NewFromTransaction(txid.Value));
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        // GET: Payees/CreateModel/{txid}
        public async Task<IActionResult> CreateModal(int txid)
        {
            try
            {
                if (txid <= 0)
                    throw new ArgumentException();

                ViewData["TXID"] = txid;

                return PartialView("CreatePartial", await _repository.NewFromTransaction(txid));
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // POST: Payees/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Create([Bind("ID,Name,Category,SubCategory")] Payee payee)
        {
            try
            {
                if (!ModelState.IsValid)
                    throw new InvalidOperationException();

                await _repository.AddAsync(payee);

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // GET: Payees/Edit/5
        public async Task<IActionResult> Edit(int? id) => await Details(id);

        // GET: Payees/EditModal/5
        public async Task<IActionResult> EditModal(int? id)
        {
            try
            {
                return PartialView("EditPartial", await _repository.GetByIdAsync(id));
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // POST: Payees/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,Category,SubCategory")] Payee item)
        {
            try
            {
                if (id != item.ID)
                    throw new ArgumentException();

                if (!ModelState.IsValid)
                    throw new InvalidOperationException();

                await _repository.UpdateAsync(item);

                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // POST: Payees/BulkEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> BulkEdit(string Category)
        {
            try
            {
                await _repository.BulkEdit(Category);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // GET: Payees/Delete/5
        public async Task<IActionResult> Delete(int? id) => await Details(id);

        // POST: Payees/Delete/5
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
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
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
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // GET: Payees/Download
        [ActionName("Download")]
        public Task<IActionResult> Download()
        {
            try
            {
                var stream = _repository.AsSpreadsheet();

                var result = File(stream, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName: $"Payee.xlsx");

                // Need to return a task to meet the IControllerBase interface
                return Task.FromResult(result as IActionResult);
            }
            catch (Exception ex)
            {
                return Task.FromResult(StatusCode(500, ex.Message) as IActionResult);
            }
        }
        Task<IActionResult> IController<Payee>.Create() => Create((int?)null);

        void IController<Payee>.SetErrorState() => ModelState.AddModelError("error", "test");
    }
}
