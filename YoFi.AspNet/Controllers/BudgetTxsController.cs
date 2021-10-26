using Common.AspNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Core.Repositories;
using YoFi.AspNet.Data;
using YoFi.AspNet.Models;
using YoFi.Core.Importers;

namespace YoFi.AspNet.Controllers
{
    [Authorize(Policy = "CanRead")]
    public class BudgetTxsController : Controller, IController<BudgetTx>
    {
        public static int PageSize { get; } = 25;

        private readonly BudgetTxRepository _repository;

        public BudgetTxsController(ApplicationDbContext _context)
        {
            // Baby steps toward a repository.
            // 
            // I still have to figure out how to get my repositories into dependency injection
            // WITH the proper context.
            _repository = new BudgetTxRepository(_context);
        }

        // GET: BudgetTxs
        public async Task<IActionResult> Index(string q = null, int? p = null)
        {
            var result = _repository.OrderedQuery;

            //
            // Process QUERY (Q) parameters
            //

            ViewData["Query"] = q;

            if (!string.IsNullOrEmpty(q))
            {
                // Look for term anywhere
                result = result.Where(x =>
                    x.Category.Contains(q)
                );
            }

            //
            // Process PAGE (P) parameters
            //

            var divider = new PageDivider() { PageSize = PageSize };
            result = await divider.ItemsForPage(result, p);
            ViewData[nameof(PageDivider)] = divider;

            // Show the index
            return View(await result.ToListAsync());
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
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // GET: BudgetTxs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: BudgetTxs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Create([Bind("ID,Amount,Timestamp,Category")] BudgetTx item)
        {
            try
            {
                if (!ModelState.IsValid)
                    throw new InvalidOperationException();

                await _repository.AddAsync(item);

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

        // GET: BudgetTxs/Edit/5
        public async Task<IActionResult> Edit(int? id) => await Details(id);

        // POST: BudgetTxs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Amount,Timestamp,Category")] BudgetTx item)
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
                return View(item);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var exists = await _repository.TestExistsByIdAsync(item.ID);

                if (exists)
                    return NotFound();
                else
                    return StatusCode(500, ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
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

                var importer = new BudgetTxImporter(_repository);

                foreach (var file in files)
                {
                    if (file.FileName.ToLower().EndsWith(".xlsx"))
                    {
                        using var stream = file.OpenReadStream();
                        importer.LoadFromXlsx(stream);
                    }
                }

                var imported = await importer.ProcessAsync();

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

        // GET: BudgetTxs/Download
        public Task<IActionResult> Download()
        {
            try
            {
                var stream = _repository.AsSpreadsheet();

                var result = File(stream, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName: $"BudgetTx.xlsx");

                // Need to return a task to meet the IControllerBase interface
                return Task.FromResult(result as IActionResult);
            }
            catch (Exception ex)
            {
                return Task.FromResult(StatusCode(500, ex.Message) as IActionResult);
            }
        }

        Task<IActionResult> IController<BudgetTx>.Index() => Index();
    }
}
