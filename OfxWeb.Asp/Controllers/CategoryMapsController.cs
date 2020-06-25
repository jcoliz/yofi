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

namespace OfxWeb.Asp.Controllers
{
    public class CategoryMapsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryMapsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CategoryMaps
        public async Task<IActionResult> Index()
        {
            return View(await _context.CategoryMaps.ToListAsync());
        }

        // GET: CategoryMaps/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoryMap = await _context.CategoryMaps
                .FirstOrDefaultAsync(m => m.ID == id);
            if (categoryMap == null)
            {
                return NotFound();
            }

            return View(categoryMap);
        }

        // GET: CategoryMaps/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CategoryMaps/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Category,SubCategory,Key1,Key2,Key3")] CategoryMap categoryMap)
        {
            if (ModelState.IsValid)
            {
                _context.Add(categoryMap);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(categoryMap);
        }

        // GET: CategoryMaps/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoryMap = await _context.CategoryMaps.FindAsync(id);
            if (categoryMap == null)
            {
                return NotFound();
            }
            return View(categoryMap);
        }

        // POST: CategoryMaps/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Category,SubCategory,Key1,Key2,Key3")] CategoryMap categoryMap)
        {
            if (id != categoryMap.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(categoryMap);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryMapExists(categoryMap.ID))
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
            return View(categoryMap);
        }

        // GET: CategoryMaps/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoryMap = await _context.CategoryMaps
                .FirstOrDefaultAsync(m => m.ID == id);
            if (categoryMap == null)
            {
                return NotFound();
            }

            return View(categoryMap);
        }

        // POST: CategoryMaps/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoryMap = await _context.CategoryMaps.FindAsync(id);
            _context.CategoryMaps.Remove(categoryMap);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            var incoming = new HashSet<Models.CategoryMap>();
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
                            var worksheet = excel.Workbook.Worksheets.Where(x => x.Name == "CategoryMaps").Single();
                            worksheet.ExtractInto(incoming);
                        }
                    }
                }

                // Remove lines which already have an ID
                // TODO: This doesn't actually work. Not sure why, cuz debugger isn't working either :(
                incoming.RemoveWhere(x => x.ID != 0);

                // Add resulting transactions

                await _context.AddRangeAsync(incoming);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

            return View(incoming.OrderBy(x => x.Category).ThenBy(x => x.SubCategory).ThenBy(x => x.Key1).ThenBy(x => x.Key2).ThenBy(x => x.Key3));
        }

        // GET: CategoryMaps/Download
        [ActionName("Download")]
        public async Task<IActionResult> Download()
        {
            const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            try
            {
                var objecttype = "CategoryMaps";
                var lines = await _context.CategoryMaps.OrderBy(x => x.Category).ThenBy(x => x.SubCategory).ThenBy(x => x.Key1).ThenBy(x => x.Key2).ThenBy(x => x.Key3).ToListAsync();

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

        private bool CategoryMapExists(int id)
        {
            return _context.CategoryMaps.Any(e => e.ID == id);
        }
    }
}
