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
    public class PayeesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PayeesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Payees
        public async Task<IActionResult> Index()
        {
            return View(await _context.Payees.ToListAsync());
        }

        // GET: Payees/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payee = await _context.Payees
                .SingleOrDefaultAsync(m => m.Name == id);
            if (payee == null)
            {
                return NotFound();
            }

            return View(payee);
        }

        // GET: Payees/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Payees/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Category,SubCategory")] Payee payee)
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
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payee = await _context.Payees.SingleOrDefaultAsync(m => m.Name == id);
            if (payee == null)
            {
                return NotFound();
            }
            return View(payee);
        }

        // POST: Payees/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Name,Category,SubCategory")] Payee payee)
        {
            if (id != payee.Name)
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
                    if (!PayeeExists(payee.Name))
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

        // GET: Payees/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payee = await _context.Payees
                .SingleOrDefaultAsync(m => m.Name == id);
            if (payee == null)
            {
                return NotFound();
            }

            return View(payee);
        }

        // POST: Payees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var payee = await _context.Payees.SingleOrDefaultAsync(m => m.Name == id);
            _context.Payees.Remove(payee);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PayeeExists(string id)
        {
            return _context.Payees.Any(e => e.Name == id);
        }
    }
}
