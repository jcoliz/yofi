using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.AspNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using YoFi.AspNet.Data;
using YoFi.Core.Models;

namespace YoFi.AspNet.Controllers
{
    [Authorize(Policy = "CanRead")]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Edit([Bind("ID,TransactionID,Amount,Category,SubCategory,Memo")] Split split)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(split);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!_context.Splits.Any(e => e.ID == split.ID))
                        return NotFound();
                    else
                        return StatusCode(500,ex.Message);
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
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var split = await _context.Splits.SingleOrDefaultAsync(m => m.ID == id);
            var category = split.Category;
            var txid = split.TransactionID;

            _context.Splits.Remove(split);
            await _context.SaveChangesAsync();

            var tx = _context.Transactions.Where(x => x.ID == txid).FirstOrDefault();
            if (tx?.HasSplits == false)
            {
                tx.Category = category;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Edit", "Transactions", new { id = txid });
        }

        void IController<Split>.SetErrorState() => ModelState.AddModelError("error", "test");

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

        Task<IActionResult> IController<Split>.Create() =>
            throw new NotImplementedException();

        Task<IActionResult> IController<Split>.Edit(int id, Split item) =>
            Edit(item);
    }
}
