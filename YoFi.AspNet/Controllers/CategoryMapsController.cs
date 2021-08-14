using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoFi.AspNet.Data;
using YoFi.AspNet.Models;
using Common.AspNet;
using YoFi.AspNet.Common;
using System.IO;

namespace YoFi.AspNet.Controllers
{
    public class CategoryMapsController : Controller, IController<CategoryMap>
    {
        private readonly ApplicationDbContext _context;

        public CategoryMapsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CategoryMaps
        public async Task<IActionResult> Index()
        {
            return View(await _context.CategoryMaps.OrderBy(x => x.Category).ThenBy(x => x.SubCategory).ThenBy(x => x.Key1).ThenBy(x => x.Key2).ThenBy(x => x.Key3).ToListAsync());
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
            IEnumerable<CategoryMap> result = Enumerable.Empty<CategoryMap>();
            try
            {
                // Extract submitted file into a list objects

                foreach (var file in files)
                {
                    if (file.FileName.ToLower().EndsWith(".xlsx"))
                    {
                        using (var stream = file.OpenReadStream())
                        using (var ssr = new SpreadsheetReader())
                        {
                            ssr.Open(stream);
                            var items = ssr.Read<CategoryMap>();
                            incoming.UnionWith(items);
                        }
                    }
                }

                // Remove duplicate transactions.
                result = incoming.Except(_context.CategoryMaps).ToList();

                // Add remaining transactions
                await _context.AddRangeAsync(result);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

            return View(result.OrderBy(x => x.Category).ThenBy(x => x.SubCategory).ThenBy(x => x.Key1).ThenBy(x => x.Key2).ThenBy(x => x.Key3));
        }

        // GET: CategoryMaps/Download
        [ActionName("Download")]
        public Task<IActionResult> Download()
        {
            try
            {
                var items = _context.CategoryMaps.OrderBy(x => x.Category).ThenBy(x => x.SubCategory).ThenBy(x => x.Key1).ThenBy(x => x.Key2).ThenBy(x => x.Key3);

                FileStreamResult result = null;
                var stream = new MemoryStream();
                using (var ssw = new SpreadsheetWriter())
                {
                    ssw.Open(stream);
                    ssw.Write(items);
                    stream.Seek(0, SeekOrigin.Begin);
                    result = File(stream, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName: $"{ssw.SheetNames.First()}.xlsx");
                }

                // Need to return a task to meet the IControllerBase interface
                return Task.FromResult(result as IActionResult);
            }
            catch (Exception)
            {
                return Task.FromResult(NotFound() as IActionResult);
            }
        }

        private bool CategoryMapExists(int id)
        {
            return _context.CategoryMaps.Any(e => e.ID == id);
        }
    }
}
