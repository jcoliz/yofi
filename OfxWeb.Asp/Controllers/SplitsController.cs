using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;

namespace OfxWeb.Asp
{
    public class SplitsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SplitsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Splits
        public async Task<IActionResult> Index()
        {
            return View(await _context.Splits.ToListAsync());
        }

        // GET: Splits/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var split = await _context.Splits
                .SingleOrDefaultAsync(m => m.ID == id);
            if (split == null)
            {
                return NotFound();
            }

            return View(split);
        }

        // GET: Splits/Create
        public IActionResult Create(int? txid)
        {
            if (txid.HasValue)
            {
                return View(new Split() { TransactionID = txid.Value });
            }
            else
                return View();
        }

        // POST: Splits/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,TransactionID,Amount,Category,SubCategory,Memo")] Split split)
        {
            if (ModelState.IsValid)
            {
                _context.Add(split);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(split);
        }

        // GET: Splits/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var split = await _context.Splits.SingleOrDefaultAsync(m => m.ID == id);
            if (split == null)
            {
                return NotFound();
            }
            return View(split);
        }

        // POST: Splits/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,TransactionID,Amount,Category,SubCategory,Memo")] Split split)
        {
            if (id != split.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(split);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SplitExists(split.ID))
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
            return View(split);
        }

        // GET: Splits/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var split = await _context.Splits
                .SingleOrDefaultAsync(m => m.ID == id);
            if (split == null)
            {
                return NotFound();
            }

            return View(split);
        }

        // POST: Splits/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var split = await _context.Splits.SingleOrDefaultAsync(m => m.ID == id);
            _context.Splits.Remove(split);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SplitExists(int id)
        {
            return _context.Splits.Any(e => e.ID == id);
        }
    }
}
