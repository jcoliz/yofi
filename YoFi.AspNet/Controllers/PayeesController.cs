using Common.AspNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Common;
using YoFi.AspNet.Data;
using YoFi.AspNet.Models;

namespace YoFi.AspNet.Controllers
{
    public class PayeesController : Controller, IController<Payee>
    {
        public static int PageSize { get; } = 25;

        private readonly ApplicationDbContext _context;

        public PayeesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Payees
        public async Task<IActionResult> Index(string q = null, string v = null, int? p = null)
        {
            //
            // Process QUERY (Q) parameters
            //

            var result = _context.Payees.OrderBy(x => x.Category).ThenBy(x => x.SubCategory).ThenBy(x => x.Name).AsQueryable();

            ViewData["Query"] = q;

            if (!string.IsNullOrEmpty(q))
            {
                // Look for term anywhere
                result = result.Where(x =>
                    x.Category.Contains(q) ||
                    x.Name.Contains(q)
                );
            }

            //
            // Process VIEW (V) parameters
            //

            ViewData["ViewP"] = v;
            bool showSelected = v?.ToLowerInvariant().Contains("s") == true;
            ViewData["ShowSelected"] = showSelected;
            ViewData["ToggleSelected"] = showSelected ? null : "s";

            //
            // Process PAGE (P) parameters
            //

            if (!p.HasValue)
                p = 1;
            else
                ViewData["Page"] = p;

            var count = await result.CountAsync();

            int offset = (p.Value - 1) * PageSize;
            ViewData["PageFirstItem"] = offset + 1;
            ViewData["PageLastItem"] = Math.Min(count, offset + PageSize);
            ViewData["PageTotalItems"] = count;

            if (count > PageSize)
            {
                result = result.Skip(offset).Take(PageSize);

                if (p > 1)
                    ViewData["PreviousPage"] = p.Value - 1;
                else
                    if ((p + 1) * PageSize < count)
                    ViewData["NextNextPage"] = p.Value + 2;

                if (p * PageSize < count)
                    ViewData["NextPage"] = p.Value + 1;
                else
                    if (p > 2)
                    ViewData["PreviousPreviousPage"] = p.Value - 2;

                if (p > 2)
                    ViewData["FirstPage"] = 1;

                if ((p + 1) * PageSize < count)
                    ViewData["LastPage"] = 1 + (count - 1) / PageSize;
            }

            return View(await result.ToListAsync());
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
        [ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            var incoming = new HashSet<Models.Payee>(new PayeeNameComparer());
            IEnumerable<Payee> result = Enumerable.Empty<Payee>();
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
                            var items = ssr.Read<Payee>();
                            incoming.UnionWith(items);
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
            try
            {
                var items = await _context.Payees.OrderBy(x => x.Category).ThenBy(x=>x.SubCategory).ThenBy(x=>x.Name).ToListAsync();

                if (mapped ?? false)
                {
                    var maptable = new CategoryMapper(_context.CategoryMaps);
                    foreach (var item in items)
                        maptable.MapObject(item);
                }

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
                return result;
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
