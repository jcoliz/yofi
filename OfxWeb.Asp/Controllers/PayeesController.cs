using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfxWeb.Asp.Controllers;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using Common.AspNetCore;

namespace OfxWeb.Asp
{
    public class PayeesController : Controller, IController<Payee>
    {
        private readonly ApplicationDbContext _context;

        public PayeesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Payees
        public async Task<IActionResult> Index(string search)
        {
            bool showSelected = false;

            if (!String.IsNullOrEmpty(search))
            {
                var terms = search.Split(',');
                foreach (var term in terms)
                {
                    if (term[0] == 'Z')
                    {
                        showSelected = (term[1] == '+');
                    }
                }
            }

            ViewData["ShowSelected"] = showSelected;
            ViewData["CurrentFilterToggleSelected"] = $"Z{(showSelected ? '-' : '+')}";

            return View(await _context.Payees.OrderBy(x=>x.Category).ThenBy(x=>x.SubCategory).ThenBy(x=>x.Name).ToListAsync());
        }
        Task<IActionResult> IController<Payee>.Index() => Index(string.Empty);

        // GET: Payees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payee = await _context.Payees
                .SingleOrDefaultAsync(m => m.ID == id);
            if (payee == null)
            {
                return NotFound();
            }

            return View(payee);
        }

        // GET: Payees/Create
        public async Task<IActionResult> Create(int? txid)
        {
            if (txid.HasValue)
            {
                var transaction = await _context.Transactions.Where(x => x.ID == txid.Value).SingleOrDefaultAsync();

                if (transaction == null)
                    return NotFound();

                var payee = new Payee() { Category = transaction.Category, Name = transaction.Payee.Trim(), SubCategory = transaction.SubCategory };
                return View(payee);
            }

            return View();
        }
        // GET: Payees/CreateModel/{txid}
        public async Task<IActionResult> CreateModal(int id)
        {
            if (id > 0)
            {
                ViewData["TXID"] = id;

                var transaction = await _context.Transactions.Where(x => x.ID == id).SingleOrDefaultAsync();

                if (transaction == null)
                    return NotFound();

                var payee = new Payee() { Category = transaction.Category, Name = transaction.Payee.Trim(), SubCategory = transaction.SubCategory };
                return PartialView("CreatePartial",payee);
            }

            return View();
        }

        // POST: Payees/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Name,Category,SubCategory")] Payee payee)
        {
            if (ModelState.IsValid)
            {
                _context.Add(payee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(payee);
        }

        // GET: Payees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payee = await _context.Payees.SingleOrDefaultAsync(m => m.ID == id);
            if (payee == null)
            {
                return NotFound();
            }
            return View(payee);
        }

        // GET: Payees/EditModal/5
        public async Task<IActionResult> EditModal(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payee = await _context.Payees.SingleOrDefaultAsync(m => m.ID == id);
            if (payee == null)
            {
                return NotFound();
            }
            return PartialView("EditPartial", payee);
        }

        // POST: Payees/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,Category,SubCategory")] Payee payee)
        {
            if (id != payee.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(payee);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PayeeExists(payee.ID))
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
            return View(payee);
        }

        // POST: Payees/BulkEdit
        [HttpPost]
        public async Task<IActionResult> BulkEdit(string Category, string SubCategory)
        {
            var result = from s in _context.Payees
                         where s.Selected == true
                         select s;

            var list = await result.ToListAsync();

            foreach (var item in list)
            {
                if (!string.IsNullOrEmpty(Category))
                    item.Category = Category;

                if (!string.IsNullOrEmpty(SubCategory))
                {
                    if ("-" == SubCategory)
                    {
                        item.SubCategory = string.Empty;
                    }
                    else
                    {
                        item.SubCategory = SubCategory;
                    }
                }

                item.Selected = false;
            }
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Payees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payee = await _context.Payees
                .SingleOrDefaultAsync(m => m.ID == id);
            if (payee == null)
            {
                return NotFound();
            }

            return View(payee);
        }

        // POST: Payees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payee = await _context.Payees.SingleOrDefaultAsync(m => m.ID == id);
            _context.Payees.Remove(payee);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            var incoming = new HashSet<Models.Payee>(new PayeeNameComparer());
            IEnumerable<Payee> result = Enumerable.Empty<Payee>();
            try
            {
                // Extract submitted file into a list objects

                foreach (var formFile in files)
                {
                    if (formFile.FileName.ToLower().EndsWith(".xlsx"))
                    {
                        using (var stream = formFile.OpenReadStream())
                        {
                            var excel = new ExcelPackage(stream);
                            var worksheet = excel.Workbook.Worksheets.Where(x => x.Name == "Payees").Single();
                            worksheet.ExtractInto(incoming);
                        }
                    }
                }

                // Remove duplicate transactions.
                result = incoming.Except(_context.Payees).ToList();

                // Fix up the remaining names
                foreach (var item in result)
                    item.FixupName();

                // Add remaining transactions
                await _context.AddRangeAsync(result);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

            return View(result.OrderBy(x => x.Category).ThenBy(x=>x.SubCategory));
        }

        // GET: Payees/Download
        [ActionName("Download")]
        public async Task<IActionResult> Download(bool? mapped = false)
        {
            const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            try
            {
                var objecttype = "Payees";
                var lines = await _context.Payees.OrderBy(x => x.Category).ThenBy(x=>x.SubCategory).ThenBy(x=>x.Name).ToListAsync();

                if (mapped ?? false)
                {
                    var maptable = new CategoryMapper(_context.CategoryMaps);
                    foreach (var item in lines)
                        maptable.MapObject(item);
                }

                byte[] reportBytes;
                using (var package = new ExcelPackage())
                {
                    package.Workbook.Properties.Title = objecttype;
                    package.Workbook.Properties.Author = "coliz.com";
                    package.Workbook.Properties.Subject = objecttype;
                    package.Workbook.Properties.Keywords = objecttype;

                    var worksheet = package.Workbook.Worksheets.Add(objecttype);
                    int rows, cols;
                    worksheet.PopulateFrom(lines, out rows, out cols);

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

        private bool PayeeExists(int id)
        {
            return _context.Payees.Any(e => e.ID == id);
        }

        Task<IActionResult> IController<Payee>.Download() => Download(false);
    }

    class PayeeNameComparer : IEqualityComparer<Models.Payee>
    {
        public bool Equals(Models.Payee x, Models.Payee y) => x.Name == y.Name;
        public int GetHashCode(Models.Payee obj) => obj.Name.GetHashCode();
    }
}
