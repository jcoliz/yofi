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
                            var worksheet = excel.Workbook.Worksheets.Where(x => x.Name == "BudgetTxs").Single();
                            worksheet.ExtractInto(incoming);
                        }
                    }
                }

                // Query for matching transactions.

                // Trying a less efficient approach
                // DESIRED:
                // var existing = await _context.BudgetTxs.Where(x => incoming.Contains(x)).ToListAsync();
                // ACTUAL:
                var all = await _context.BudgetTxs.ToListAsync();
                var existing = all.Where(x => incoming.Contains(x));

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

        // GET: Transactions/Download
        [ActionName("Download")]
        public async Task<IActionResult> Download()
        {
            const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            try
            {
                var objecttype = "BudgetTxs";
                var transactions = await _context.BudgetTxs.Where(x => x.Timestamp.Year == 2018).OrderBy(x => x.Timestamp).ThenBy(x=>x.Category).ToListAsync();

                byte[] reportBytes;
                using (var package = new ExcelPackage())
                {
                    package.Workbook.Properties.Title = objecttype;
                    package.Workbook.Properties.Author = "coliz.com";
                    package.Workbook.Properties.Subject = objecttype;
                    package.Workbook.Properties.Keywords = objecttype;

                    var worksheet = package.Workbook.Worksheets.Add(objecttype);
                    int rows, cols;
                    worksheet.PopulateFrom(transactions, out rows, out cols);

                    var tbl = worksheet.Tables.Add(new ExcelAddressBase(fromRow: 1, fromCol: 1, toRow: rows, toColumn: cols), objecttype);
                    tbl.ShowHeader = true;
                    tbl.TableStyle = OfficeOpenXml.Table.TableStyles.Dark9;

                    reportBytes = package.GetAsByteArray();
                }

                return File(reportBytes, XlsxContentType, $"{objecttype}.xlsx");
            }
            catch (Exception)
            {
                return NotFound();
            }
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
