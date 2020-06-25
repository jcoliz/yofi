using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

        private bool CategoryMapExists(int id)
        {
            return _context.CategoryMaps.Any(e => e.ID == id);
        }
    }
}
