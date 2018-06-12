using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;

namespace OfxWeb.Asp
{
    public class BudgetTxsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BudgetTxsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: BudgetTxs
        public async Task<IActionResult> Index()
        {
            return View(await _context.BudgetTxs.OrderBy(x => x.Timestamp.Year).ThenBy(x => x.Timestamp.Month).ThenBy(x => x.Category).ToListAsync());
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
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var budgetTx = await _context.BudgetTxs.SingleOrDefaultAsync(m => m.ID == id);
            _context.BudgetTxs.Remove(budgetTx);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            var incoming = new HashSet<Models.BudgetTx>(new BudgetTxComparer());
            try
            {
                foreach (var formFile in files)
                {
                    if (formFile.FileName.ToLower().EndsWith(".xlsx"))
                    {
                        using (var stream = formFile.OpenReadStream())
                        {
                            var excel = new ExcelPackage(stream);
                            var worksheet = excel.Workbook.Worksheets.Where(x => x.Name == "Budget Tx").Single();

                            var cols = new List<String>();

                            // Read headers
                            for (int i = 1; i <= worksheet.Dimension.Columns; i++)
                            {
                                cols.Add(worksheet.Cells[1, i].Text);
                            }
                            var DateCol = 1 + cols.IndexOf("Date");
                            var CategoryCol = 1 + cols.IndexOf("Category");
                            var AmountCol = 1 + cols.IndexOf("Amount");

                            // Read rows
                            for (int i = 2; i <= worksheet.Dimension.Rows; i++)
                            {
                                var Date = (DateTime)worksheet.Cells[i, DateCol].Value;
                                var Category = worksheet.Cells[i, CategoryCol].Text.Trim();
                                var Amount = Convert.ToDecimal( (double)worksheet.Cells[i, AmountCol].Value );

                                var tx = new Models.BudgetTx() { Timestamp = Date , Category = Category, Amount = Amount };

                                incoming.Add(tx);
                            }
                        }
                    }
                }

                // Query for matching transactions.

                var existing = await _context.BudgetTxs.Where(x => incoming.Contains(x)).ToListAsync();

                // Removed duplicate transactions.

                incoming.ExceptWith(existing);

                // Add resulting transactions

                await _context.AddRangeAsync(incoming);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

            return View(incoming.OrderBy(x => x.Timestamp.Year).ThenBy(x=>x.Timestamp.Month).ThenBy(x => x.Category));
        }

        private bool BudgetTxExists(int id)
        {
            return _context.BudgetTxs.Any(e => e.ID == id);
        }
    }

    class BudgetTxComparer: IEqualityComparer<Models.BudgetTx>
    {
        public bool Equals(Models.BudgetTx x, Models.BudgetTx y) => x.Timestamp.Year == y.Timestamp.Year && x.Timestamp.Month == y.Timestamp.Month && x.Category == y.Category;
        public int GetHashCode(Models.BudgetTx obj) => (obj.Timestamp.Year * 12 + obj.Timestamp.Month ) ^ obj.Category.GetHashCode();
    }
}
