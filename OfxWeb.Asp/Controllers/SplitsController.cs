using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;

namespace OfxWeb.Asp.Controllers
{
    public class SplitsController : Controller, IController<Split>
    {
        private readonly ApplicationDbContext _context;

        public SplitsController(ApplicationDbContext context)
        {
            _context = context;
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
                return RedirectToAction("Edit","Transactions", new { id = split.TransactionID });
            }
            return View(split);
        }

        // GET: Splits/Delete/5
        public async Task<IActionResult> Delete(int? id) => await Edit(id);

        // POST: Splits/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var split = await _context.Splits.SingleOrDefaultAsync(m => m.ID == id);
            var txid = split.TransactionID;

            _context.Splits.Remove(split);
            await _context.SaveChangesAsync();
            return RedirectToAction("Edit", "Transactions", new { id = txid });

        }

        private bool SplitExists(int id)
        {
            return _context.Splits.Any(e => e.ID == id);
        }

        Task<IActionResult> IController<Split>.Download() =>
            throw new NotImplementedException();

        Task<IActionResult> IController<Split>.Upload(List<IFormFile> files) =>
            throw new NotImplementedException();

        Task<IActionResult> IController<Split>.Index() =>
            throw new NotImplementedException();

        Task<IActionResult> IController<Split>.Details(int? id) =>
            throw new NotImplementedException();

        Task<IActionResult> IController<Split>.Create(Split item) =>
            throw new NotImplementedException();
    }
}
