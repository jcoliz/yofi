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
using YoFi.AspNet.Data;
using YoFi.AspNet.Models;

namespace YoFi.AspNet.Controllers
{
    [Authorize(Policy = "CanRead")]
    public class PayeesController : Controller, IController<Payee>
    {
        public static int PageSize { get; } = 25;

        private readonly ApplicationDbContext _context;

        public PayeesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Payees
        public async Task<IActionResult> Index(string q = null, string v = null, int? p = null)
        {
            //
            // Process QUERY (Q) parameters
            //

            var result = _context.Payees.OrderBy(x => x.Category).ThenBy(x => x.Name).AsQueryable();

            ViewData["Query"] = q;

            if (!string.IsNullOrEmpty(q))
            {
                // Look for term anywhere
                result = result.Where(x =>
                    x.Category.Contains(q) ||
                    x.Name.Contains(q)
                );
            }

            //
            // Process VIEW (V) parameters
            //

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

            return View(await result.ToListAsync());
        }

        Task<IActionResult> IController<Payee>.Index() => Index(string.Empty);

        // GET: Payees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            try
            {
                return View(await Get(id));
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

        private async Task<Payee> Get(int? id) => await _context.Payees.SingleAsync(x => x.ID == id.Value);

        // GET: Payees/Create
        public async Task<IActionResult> Create(int? txid)
        {
            try
            {
                // Create with no txid is valid. It means user is creating one from scratch
                if (!txid.HasValue)
                    return View();

                var transaction = await _context.Transactions.Where(x => x.ID == txid.Value).SingleAsync();
                var payee = new Payee() { Category = transaction.Category, Name = transaction.Payee.Trim() };

                return View(payee);
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
        public async Task<IActionResult> CreateModal(int id)
        {
            try
            {
                if (id <= 0)
                    throw new ArgumentException();

                ViewData["TXID"] = id;

                var transaction = await _context.Transactions.Where(x => x.ID == id).SingleAsync();

                var payee = new Payee() { Category = transaction.Category, Name = transaction.Payee.Trim() };
                return PartialView("CreatePartial", payee);
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

                _context.Add(payee);
                await _context.SaveChangesAsync();

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

        // GET: Payees/Edit/5
        public async Task<IActionResult> Edit(int? id) => await Details(id);

        // GET: Payees/EditModal/5
        public async Task<IActionResult> EditModal(int? id)
        {
            try
            {
                return PartialView("EditPartial", await Get(id));
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

                _context.Update(item);
                await _context.SaveChangesAsync();

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
                if (!_context.Transactions.Any(e => e.ID == item.ID))
                    return NotFound();
                else
                    return StatusCode(500, ex.Message);
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
                var result = from s in _context.Payees
                             where s.Selected == true
                             select s;

                var list = await result.ToListAsync();

                foreach (var item in list)
                {
                    if (!string.IsNullOrEmpty(Category))
                        item.Category = Category;

                    item.Selected = false;
                }
                await _context.SaveChangesAsync();

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
                var item = await Get(id);

                _context.Payees.Remove(item);
                await _context.SaveChangesAsync();

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
            var incoming = new HashSet<Models.Payee>(new PayeeNameComparer());
            IEnumerable<Payee> result = Enumerable.Empty<Payee>();
            try
            {
                if (files == null || !files.Any())
                    throw new ApplicationException("Please choose a file to upload, first.");

                // Extract submitted file into a list objects

                foreach (var file in files)
                {
                    if (file.FileName.ToLower().EndsWith(".xlsx"))
                    {
                        using var stream = file.OpenReadStream();
                        using var ssr = new SpreadsheetReader();
                        ssr.Open(stream);
                        var items = ssr.Deserialize<Payee>(exceptproperties: new string[] { "ID" });
                        incoming.UnionWith(items);
                    }
                }

                // Remove duplicate transactions.
                result = incoming.Except(_context.Payees).ToList();

                // Fix up the remaining names
                foreach (var item in result)
                    item.RemoveWhitespaceFromName();

                // Add remaining transactions
                await _context.AddRangeAsync(result);
                await _context.SaveChangesAsync();

                return View(result.OrderBy(x => x.Category));
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
        public async Task<IActionResult> Download()
        {
            try
            {
                var items = await _context.Payees.OrderBy(x => x.Category).ThenBy(x=>x.Name).ToListAsync();

                FileStreamResult result = null;
                var stream = new MemoryStream();
                using (var ssw = new SpreadsheetWriter())
                {
                    ssw.Open(stream);
                    ssw.Serialize(items);
                }

                stream.Seek(0, SeekOrigin.Begin);
                result = File(stream, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName: $"Payees.xlsx");

                // Need to return a task to meet the IControllerBase interface
                return result;
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }

    class PayeeNameComparer : IEqualityComparer<Models.Payee>
    {
        public bool Equals(Models.Payee x, Models.Payee y) => x.Name == y.Name;
        public int GetHashCode(Models.Payee obj) => obj.Name.GetHashCode();
    }
}
