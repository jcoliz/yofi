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
    public class BudgetTxsController : Controller, IController<BudgetTx>
    {
        public static int PageSize { get; } = 25;

        private readonly ApplicationDbContext _context;

        public BudgetTxsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: BudgetTxs
        public async Task<IActionResult> Index(string q = null, int? p = null)
        {
            var result = _context.BudgetTxs.OrderByDescending(x => x.Timestamp.Year).ThenByDescending(x => x.Timestamp.Month).ThenBy(x => x.Category).AsQueryable();

            if (result.FirstOrDefault() != null)
            {
                var nextmonth = result.First().Timestamp.AddMonths(1);
                ViewData["LastMonth"] = $"Generate {nextmonth:MMMM} Budget";
            }

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
            if (id == null)
            {
                return NotFound();
            }

            var budgetTx = await _context.BudgetTxs
                .SingleOrDefaultAsync(m => m.ID == id);
            if (budgetTx == null)
            {
                return NotFound();
            }

            return View(budgetTx);
        }
#if false
        // Note that this is not currently implemented in the UI
        // Also, these days my practice is to generate all the budgettx at the beginning of the year
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate()
        {
            // Grab the whole first group
            var result = await _context.BudgetTxs.GroupBy(x => x.Timestamp).OrderByDescending(x => x.Key).FirstOrDefaultAsync();

            if (null == result)
                return NotFound();

            var timestamp = result.First().Timestamp.AddMonths(1);
            var newmonth = result.Select(x => new BudgetTx(x, timestamp));

            await _context.BudgetTxs.AddRangeAsync(newmonth);
            await _context.SaveChangesAsync();

            return Redirect("/BudgetTxs");
        }
#endif

        // GET: BudgetTxs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: BudgetTxs/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Create([Bind("ID,Amount,Timestamp,Category")] BudgetTx budgetTx)
        {
            if (ModelState.IsValid)
            {
                _context.Add(budgetTx);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(budgetTx);
        }

        // GET: BudgetTxs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var budgetTx = await _context.BudgetTxs.SingleOrDefaultAsync(m => m.ID == id);
            if (budgetTx == null)
            {
                return NotFound();
            }
            return View(budgetTx);
        }

        // POST: BudgetTxs/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Amount,Timestamp,Category")] BudgetTx budgetTx)
        {
            if (id != budgetTx.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(budgetTx);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BudgetTxExists(budgetTx.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(budgetTx);
        }

        // GET: BudgetTxs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var budgetTx = await _context.BudgetTxs
                .SingleOrDefaultAsync(m => m.ID == id);
            if (budgetTx == null)
            {
                return NotFound();
            }

            return View(budgetTx);
        }

        // POST: BudgetTxs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var budgetTx = await _context.BudgetTxs.SingleOrDefaultAsync(m => m.ID == id);
            _context.BudgetTxs.Remove(budgetTx);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            var incoming = new HashSet<Models.BudgetTx>(new BudgetTxComparer());
            IEnumerable<BudgetTx> result = Enumerable.Empty<BudgetTx>();
            try
            {
                if (files == null || !files.Any())
                    throw new ApplicationException("Please choose a file to upload, first.");

                foreach (var file in files)
                {
                    if (file.FileName.ToLower().EndsWith(".xlsx"))
                    {
                        using var stream = file.OpenReadStream();
                        using var ssr = new SpreadsheetReader();
                        ssr.Open(stream);
                        var items = ssr.Deserialize<BudgetTx>(exceptproperties: new string[] { "ID" });
                        incoming.UnionWith(items);
                    }
                }

                // Remove duplicate transactions.
                result = incoming.Except(_context.BudgetTxs).ToList();

                // Add remaining transactions
                await _context.AddRangeAsync(result);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return View(result.OrderBy(x => x.Timestamp.Year).ThenBy(x=>x.Timestamp.Month).ThenBy(x => x.Category));
        }

        // GET: BudgetTxs/Download
        public Task<IActionResult> Download()
        {
            try
            {
                var items = _context.BudgetTxs.OrderBy(x => x.Timestamp).ThenBy(x=>x.Category);

                FileStreamResult result = null;
                var stream = new MemoryStream();
                using (var ssw = new SpreadsheetWriter())
                {
                    ssw.Open(stream);
                    ssw.Serialize(items);
                }

                stream.Seek(0, SeekOrigin.Begin);
                result = File(stream, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName: $"BudgetTx.xlsx");

                // Need to return a task to meet the IControllerBase interface
                return Task.FromResult(result as IActionResult);
            }
            catch (Exception)
            {
                return Task.FromResult(NotFound() as IActionResult);
            }
        }

        private bool BudgetTxExists(int id)
        {
            return _context.BudgetTxs.Any(e => e.ID == id);
        }

        Task<IActionResult> IController<BudgetTx>.Index() => Index();
    }

    class BudgetTxComparer: IEqualityComparer<Models.BudgetTx>
    {
        public bool Equals(Models.BudgetTx x, Models.BudgetTx y) => x.Timestamp.Year == y.Timestamp.Year && x.Timestamp.Month == y.Timestamp.Month && x.Category == y.Category;
        public int GetHashCode(Models.BudgetTx obj) => (obj.Timestamp.Year * 12 + obj.Timestamp.Month ) ^ obj.Category.GetHashCode();
    }
}
