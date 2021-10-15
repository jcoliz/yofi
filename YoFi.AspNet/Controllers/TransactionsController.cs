using Common.AspNet;
using Common.NET;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OfxSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoFi.AspNet.Boilerplate.Models;
using YoFi.AspNet.Controllers.Reports;
using YoFi.AspNet.Data;
using YoFi.AspNet.Models;
using Transaction = YoFi.AspNet.Models.Transaction;

namespace YoFi.AspNet.Controllers
{
    [Authorize(Policy = "CanRead")]
    public class TransactionsController : Controller, IController<Models.Transaction>
    {
        #region Public Properties
        public static int PageSize { get; } = 25;

        #endregion

        #region Constructor

        public TransactionsController(ApplicationDbContext context, IConfiguration config, IPlatformAzureStorage storage = null)
        {
            _context = context;
            _storage = storage;
            _config = config;
        }

        #endregion

        #region Action Handlers

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Interprets the "q" (Query) parameter on a transactions search
        /// </summary>
        /// <remarks>
        /// Public so can be used by other controllers
        /// </remarks>
        /// <param name="result">Initial query to further refine</param>
        /// <param name="q">Query parameter</param>
        /// <returns>Resulting query refined by <paramref name="q"/></returns>
        public static IQueryable<Transaction> TransactionsForQuery(IQueryable<Transaction> result, string q)
        {
            if (!string.IsNullOrEmpty(q))
            {
                var terms = q.Split(',');

                foreach (var term in terms)
                {
                    // Look for "{key}={value}" terms
                    if (term.Length > 2 && term[1] == '=')
                    {
                        var key = term.ToLowerInvariant().First();
                        var value = term[2..];

                        switch (key)
                        {
                            // Payee
                            case 'p':
                                result = result.Where(x => x.Payee.Contains(value));
                                break;

                            // Category
                            case 'c':
                                if (value.ToLowerInvariant() == "[blank]")
                                    result = result.Where(x => string.IsNullOrEmpty(x.Category) && !x.Splits.Any());
                                else
                                    result = result.Where(x =>
                                        x.Category.Contains(value)
                                        ||
                                        x.Splits.Any(s => s.Category.Contains(value))
                                    );
                                break;

                            // Year
                            case 'y':
                                int year;
                                if (Int32.TryParse(value, out year))
                                    result = result.Where(x => x.Timestamp.Year == year);
                                break;

                            // Memo
                            case 'm':
                                result = result.Where(x => x.Memo.Contains(value));
                                break;

                            // Has Receipt
                            case 'r':
                                if (value == "0")
                                    result = result.Where(x => x.ReceiptUrl == null);
                                else if (value == "1")
                                    result = result.Where(x => x.ReceiptUrl != null);
                                break;

                            // Amount
                            case 'a':
                                if (Int32.TryParse(value, out Int32 ival))
                                {
                                    var cents = ((decimal)ival) / 100;
                                    result = result.Where(x => x.Amount == (decimal)ival || x.Amount == cents || x.Amount == -(decimal)ival || x.Amount == -cents);
                                }
                                else if (decimal.TryParse(value, out decimal dval))
                                {
                                    result = result.Where(x => x.Amount == dval || x.Amount == -dval);
                                }
                                break;

                            // Date: On this day or up to a week later
                            case 'd':
                                DateTime? dtval = null;
                                if (Int32.TryParse(value, out ival) && ival >= 101 && ival <= 1231)
                                {
                                    dtval = new DateTime(DateTime.Now.Year, ival / 100, ival % 100);
                                }
                                else if (DateTime.TryParse(value, out DateTime dtvalout))
                                {
                                    dtval = dtvalout;
                                }
                                if (dtval.HasValue)
                                {
                                    result = result.Where(x => x.Timestamp >= dtval.Value && x.Timestamp < dtval.Value.AddDays(7));
                                }

                                break;
                        }
                    }
                    else if (Int32.TryParse(term, out Int32 intval))
                    {
                        // If this is an integer search term, there's a lot of places it can be. It will
                        // the the SAME as the text search, below, plus amount or date.

                        // One tricky thing is figuring out of it's a valid date
                        DateTime? dtval = null;
                        try
                        {
                            dtval = new DateTime(DateTime.Now.Year, intval / 100, intval % 100);
                        }
                        catch
                        {
                            // Any issues, we'll leave dtval as null
                        }

                        if (dtval.HasValue)
                        {
                            result = result.Where(x =>
                                x.Category.Contains(term) ||
                                x.Memo.Contains(term) ||
                                x.Payee.Contains(term) ||
                                x.Amount == (decimal)intval ||
                                x.Amount == ((decimal)intval) / 100 ||
                                x.Amount == -(decimal)intval ||
                                x.Amount == -((decimal)intval) / 100 ||
                                (x.Timestamp >= dtval && x.Timestamp <= dtval.Value.AddDays(7)) ||
                                x.Splits.Any(s =>
                                    s.Category.Contains(term) ||
                                    s.Memo.Contains(term) ||
                                    s.Amount == (decimal)intval ||
                                    s.Amount == ((decimal)intval) / 100 ||
                                    s.Amount == -(decimal)intval ||
                                    s.Amount == -((decimal)intval) / 100
                                )
                            );
                        }
                        else
                        {
                            result = result.Where(x =>
                                x.Category.Contains(term) ||
                                x.Memo.Contains(term) ||
                                x.Payee.Contains(term) ||
                                x.Amount == (decimal)intval ||
                                x.Amount == ((decimal)intval) / 100 ||
                                x.Amount == -(decimal)intval ||
                                x.Amount == -((decimal)intval) / 100 ||
                                x.Splits.Any(s =>
                                    s.Category.Contains(term) ||
                                    s.Memo.Contains(term) ||
                                    s.Amount == (decimal)intval ||
                                    s.Amount == ((decimal)intval) / 100 ||
                                    s.Amount == -(decimal)intval ||
                                    s.Amount == -((decimal)intval) / 100
                                )
                            );
                        }

                    }
                    else
                    {
                        // Look for term anywhere
                        result = result.Where(x =>
                            x.Category.Contains(term) ||
                            x.Memo.Contains(term) ||
                            x.Payee.Contains(term) ||
                            x.Splits.Any(s =>
                                s.Category.Contains(term) ||
                                s.Memo.Contains(term)
                            )
                        );

                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Interprets the "v" (View) parameter on a transactions search
        /// </summary>
        /// <remarks>
        /// Public so can be used by other controllers.
        /// </remarks>
        /// <param name="result">Initial query to further refine</param>
        /// <param name="p">View parameter</param>
        /// <returns>Resulting query refined by <paramref name="v"/></returns>
        public static IQueryable<Transaction> TransactionsForViewspec(IQueryable<Transaction> result, string v, ViewDataDictionary ViewData, out bool showHidden, out bool showSelected)
        {
            showHidden = v?.ToLowerInvariant().Contains("h") == true;
            showSelected = v?.ToLowerInvariant().Contains("s") == true;

            ViewData["ShowHidden"] = showHidden;
            ViewData["ShowSelected"] = showSelected;
            ViewData["ToggleHidden"] = (showHidden ? string.Empty : "h") + (showSelected ? "s" : string.Empty);
            ViewData["ToggleSelected"] = (showHidden ? "h" : string.Empty) + (showSelected ? string.Empty : "s");

            if (!showHidden)
                return result.Where(x => x.Hidden != true);
            else
                return result;
        } 

        /// <summary>
        /// Interprets the "o" (Order) parameter on a transactions search
        /// </summary>
        /// <remarks>
        /// Public so can be used by other controllers.
        /// </remarks>
        /// <param name="result">Initial query to further refine</param>
        /// <param name="p">Order parameter</param>
        /// <returns>Resulting query refined by <paramref name="o"/></returns>
        public static IQueryable<Transaction> TransactionsForOrdering(IQueryable<Transaction> result, string o) => o switch 
        { 
            // Coverlet finds cyclomatic complexity of 42 in this function!!?? No clue why it's not just 10.
            "aa" => result.OrderBy(s => s.Amount),
            "ad" => result.OrderByDescending(s => s.Amount),
            "ra" => result.OrderBy(s => s.BankReference), 
            "rd" => result.OrderByDescending(s => s.BankReference), 
            "pa" => result.OrderBy(s => s.Payee), 
            "pd" => result.OrderByDescending(s => s.Payee), 
            "ca" => result.OrderBy(s => s.Category),
            "cd" => result.OrderByDescending(s => s.Category),
            "da" => result.OrderBy(s => s.Timestamp).ThenBy(s => s.BankReference), 
                _ => result.OrderByDescending(s => s.Timestamp).ThenByDescending(s => s.BankReference)
        };

        /// <summary>
        /// Fetch list of transactions for display
        /// </summary>
        /// <param name="o">Order of transactions</param>
        /// <param name="p">Page number, where 1 is first page</param>
        /// <param name="q">Query (or filter) specifying which transactions</param>
        /// <param name="v">View modifiers, specifying how the view should look</param>
        /// <returns></returns>
        public async Task<IActionResult> Index(string o = null, int? p = null, string q = null, string v = null)
        {
            //
            // Process QUERY (Q) parameters
            //

            ViewData["Query"] = q;

            var result = TransactionsForQuery(_context.Transactions.Include(x => x.Splits),q);

            //
            // Process VIEW (V) parameters
            //

            ViewData["ViewP"] = v;

            result = TransactionsForViewspec(result, v, ViewData, out bool showHidden, out bool showSelected);

            //
            // Process ORDER (O) parameters
            //

            const string default_order = "dd";
            ViewData["Order"] = (o == default_order) ? null : o;

            if (string.IsNullOrEmpty(o))
                o = default_order;

            ViewData["DateSortParm"] = o == "dd" ? "da" : null; /* not "dd", which is default */;
            ViewData["PayeeSortParm"] = o == "pa" ? "pd" : "pa";
            ViewData["CategorySortParm"] = o == "ca" ? "cd" : "ca";
            ViewData["AmountSortParm"] = o == "aa" ? "as" : "aa";
            ViewData["BankReferenceSortParm"] = o == "ra" ? "rd" : "ra";

            result = TransactionsForOrdering(result, o);

            //
            // Process PAGE (P) parameters
            //

            var divider = new PageDivider() { PageSize = PageSize };
            result = await divider.ItemsForPage(result, p);
            ViewData[nameof(PageDivider)] = divider;

            // Use the Transaction object itself as a DTO, filtering out what we don't need to return

            IEnumerable<TransactionIndexDto> r;
            if (showHidden || showSelected)
            {
                // Get the long form
                r = await result.Select(t => new TransactionIndexDto()
                {
                    ID = t.ID,
                    Timestamp = t.Timestamp,
                    Payee = t.Payee,
                    Amount = t.Amount,
                    Category = t.Category,
                    Memo = t.Memo,
                    HasReceipt = t.ReceiptUrl != null,
                    HasSplits = t.Splits.Any(),
                    BankReference = t.BankReference,
                    Hidden = t.Hidden ?? false,
                    Selected = t.Selected ?? false
                }).ToListAsync();
            }
            else
            {
                // Get the shorter form
                r = await result.Select(t => new TransactionIndexDto()
                {
                    ID = t.ID,
                    Timestamp = t.Timestamp,
                    Payee = t.Payee,
                    Amount = t.Amount,
                    Category = t.Category,
                    Memo = t.Memo,
                    HasReceipt = t.ReceiptUrl != null,
                    HasSplits = t.Splits.Any(),
                }).ToListAsync();
            }

            return View(r);
        }

        /// <summary>
        /// The transaction data for Index page
        /// </summary>
        public class TransactionIndexDto
        {
            public int ID { get; set; }
            [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
            [Display(Name = "Date")]
            public DateTime Timestamp { get; set; }
            public string Payee { get; set; }
            [DisplayFormat(DataFormatString = "{0:C2}")]
            public decimal Amount { get; set; }
            public string Category { get; set; }
            public string Memo { get; set; }
            public bool HasReceipt { get; set; }
            public bool HasSplits { get; set; }

            // Only needed in some cases

            public string BankReference { get; set; }
            public bool Hidden { get; set; }
            public bool Selected { get; set; }

            // This is just for test cases, so it's a limited transaltion, just what we need for
            // certain cases.
            public static explicit operator Transaction (TransactionIndexDto o) => new Transaction()
            {
                Category = o.Category,
                Memo = o.Memo,
                Payee = o.Payee
            };

            public bool Equals(Transaction other)
            {
                return string.Equals(Payee, other.Payee) && Amount == other.Amount && Timestamp.Date == other.Timestamp.Date;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> CreateSplit(int id)
        {
            Split result = null;
            try
            {
                /*
                    1) ADD a split to the transaction in the full amount needed to get back to total amount.
                    2) COPY the Category Information over from the main, if exists
                    3) NULL out the Cat/SubCat from the transaction
                 */
                var transaction = await _context.Transactions.Include("Splits")
                    .SingleOrDefaultAsync(m => m.ID == id);

                if (transaction == null)
                    throw new KeyNotFoundException();

                result = new Split() { Category = transaction.Category };

                // Calculate the amount based on how much is remaining.

                var currentamount = transaction.Splits.Select(x => x.Amount).Sum();
                var remaining = transaction.Amount - currentamount;
                result.Amount = remaining;

                transaction.Splits.Add(result);

                // Remove the category information, that's now contained in the splits.

                transaction.Category = null;

                _context.Update(transaction);
                await _context.SaveChangesAsync();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

            return RedirectToAction("Edit", "Splits", new { id = result.ID });
        }

        public async Task<IActionResult> Details(int? id)
        {
            try
            {
                if (!id.HasValue)
                    throw new ArgumentException();

                var result = await _context.Transactions.Where(x=>x.ID == id.Value).SingleAsync();

                return View(result);
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> ProcessImported(string command)
        {
            try
            {
                if (string.IsNullOrEmpty(command))
                    throw new ArgumentException();

                var allimported = from s in _context.Transactions
                                  where s.Imported == true
                                  orderby s.Timestamp descending, s.BankReference ascending
                                  select s;

                var selected = await allimported.Where(x => true == x.Selected).ToListAsync();
                var unselected = await allimported.Where(x => true != x.Selected).ToListAsync();

                if (command == "cancel")
                {
                    _context.Transactions.RemoveRange(allimported);
                    _context.SaveChanges();
                    return RedirectToAction(nameof(Import));
                }
                else if (command == "ok")
                {
                    foreach (var item in selected)
                        item.Imported = item.Hidden = item.Selected = false;
                    _context.Transactions.RemoveRange(unselected);
                    _context.SaveChanges();
                    return RedirectToAction(nameof(Index));
                }
                else
                    throw new ArgumentException();
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        public async Task<IActionResult> Import(string highlight = null, int? p = null)
        {
            try
            {
                IQueryable<Transaction> result = from s in _context.Transactions
                                                 where s.Imported == true
                                                 orderby s.Timestamp descending, s.BankReference ascending
                                                 select s;

                //
                // Process PAGE (P) parameters
                //

                var divider = new PageDivider() { PageSize = PageSize };
                result = await divider.ItemsForPage(result, p);
                ViewData[nameof(PageDivider)] = divider;

                try
                {
                    if (!string.IsNullOrEmpty(highlight))
                    {
                        ViewData["Highlight"] = highlight.Split(':').Select(x => Convert.ToInt32(x)).ToHashSet();
                    }
                }
                catch
                {
                    // If this fails in any way, nevermind.
                }

                return View(await result.AsNoTracking().ToListAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> BulkEdit(string Category)
        {
            try
            {
                foreach (var item in _context.Transactions.Where(x => x.Selected == true))
                {
                    item.Selected = false;

                    if (!string.IsNullOrEmpty(Category))
                    {
                        // This may be a pattern-matching search, treat it like one
                        // Note that you can treat a non-pattern-matching replacement JUST LIKE a pattern
                        // matching one, it's just slower.
                        if (Category.Contains("("))
                        {
                            var originals = item.Category?.Split(":") ?? default;
                            var result = new List<string>();
                            foreach (var component in Category.Split(":"))
                            {
                                if (component.StartsWith("(") && component.EndsWith("+)"))
                                {
                                    if (Int32.TryParse(component[1..^2], out var position))
                                        if (originals.Count() >= position)
                                            result.AddRange(originals.Skip(position - 1));
                                }
                                else if (component.StartsWith("(") && component.EndsWith(")"))
                                {
                                    if (Int32.TryParse(component[1..^1], out var position))
                                        if (originals.Count() >= position)
                                            result.AddRange(originals.Skip(position - 1).Take(1));
                                }
                                else
                                    result.Add(component);
                            }

                            if (result.Any())
                                item.Category = string.Join(":", result);
                        }
                        // It's just a simple replacement
                        else
                        {
                            item.Category = Category;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        // POST: Transactions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Create([Bind("ID,Timestamp,Amount,Memo,Payee,Category,SubCategory,BankReference")] Models.Transaction transaction)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Add(transaction);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                return View(transaction);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (!id.HasValue)
                    throw new ArgumentException();

                var transaction = await _context.Transactions.Include(x => x.Splits).SingleAsync(m => m.ID == id);

                // Handle payee auto-assignment

                if (string.IsNullOrEmpty(transaction.Category))
                {
                    // See if the payee exists
                    var payee = await _context.Payees.FirstOrDefaultAsync(x => transaction.Payee.Contains(x.Name));

                    if (payee != null)
                    {
                        transaction.Category = payee.Category;
                        ViewData["AutoCategory"] = true;
                    }
                }

                // Handle error condition where splits don't add up

                var splitstotal = transaction.Splits.Select(x => x.Amount).Sum();
                ViewData["SplitsOK"] = (splitstotal == transaction.Amount);

                return View(transaction);
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        public async Task<IActionResult> EditModal(int? id)
        {
            try
            {
                // TODO: Refactor to no duplicate between here and Edit

                if (!id.HasValue)
                    throw new ArgumentException();

                var transaction = await _context.Transactions.Include(x => x.Splits).SingleAsync(m => m.ID == id);

                // Handle payee auto-assignment

                if (string.IsNullOrEmpty(transaction.Category))
                {
                    // See if the payee exists
                    var payee = await _context.Payees.FirstOrDefaultAsync(x => transaction.Payee.Contains(x.Name));

                    if (payee != null)
                    {
                        transaction.Category = payee.Category;
                        ViewData["AutoCategory"] = true;
                    }
                }

                return PartialView("EditPartial", transaction);

            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        public IActionResult DownloadPartial()
        {
            return PartialView();
        }

        // I believe this is never used. Instead, API Controller ApplyPayee is used.
#if false
        public async Task<IActionResult> ApplyPayee(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions.SingleOrDefaultAsync(m => m.ID == id);
            if (transaction == null)
            {
                return NotFound();
            }

            // Handle payee auto-assignment

            // See if the payee exists
            var payee = await _context.Payees.FirstOrDefaultAsync(x => transaction.Payee.Contains(x.Name));

            if (payee != null)
            {
                transaction.Category = payee.Category;
                transaction.SubCategory = payee.SubCategory;
                _context.Update(transaction);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
#endif
        // POST: Transactions/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Edit(int id, bool? duplicate, [Bind("ID,Timestamp,Amount,Memo,Payee,Category,SubCategory,BankReference")] Models.Transaction transaction)
        {
            try
            {
                if (id != transaction.ID && duplicate != true)
                    throw new ArgumentException();

                if (!ModelState.IsValid)
                    throw new InvalidOperationException();

                if (duplicate == true)
                {
                    transaction.ID = 0;
                    _context.Add(transaction);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Bug #846: This Edit function is not allowed to alter the
                    // ReceiptUrl. So we much preserve whatever was there.

                    var old = await _context.Transactions.Include(x => x.Splits).SingleOrDefaultAsync(m => m.ID == id);
                    if (old == null)
                    {
                        return NotFound();
                    }
                    _context.Entry(old).State = EntityState.Detached;

                    transaction.ReceiptUrl = old.ReceiptUrl;

                    _context.Update(transaction);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (InvalidOperationException)
            {
                return View(transaction);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!_context.Transactions.Any(e => e.ID == transaction.ID))
                    return NotFound();
                else
                    return StatusCode(500, ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // GET: Transactions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            try
            {
                if (!id.HasValue)
                    throw new ArgumentException();

                var result = await _context.Transactions.Where(x => x.ID == id.Value).SingleAsync();

                return View(result);
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // POST: Transactions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var transaction = await _context.Transactions.SingleAsync(m => m.ID == id);

                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> UpReceipt(List<IFormFile> files, int id)
        {
            try
            {
                if (files == null || !files.Any())
                    throw new ApplicationException("Must choose a receipt file before uploading.");

                if (files.Skip(1).Any())
                    throw new ApplicationException("Must choose a only single receipt file. Uploading multiple receipts for a single transaction is not supported.");

                if (null == _storage)
                    throw new InvalidOperationException("Unable to upload receipt. Azure Blob Storage is not configured for this application.");

                var transaction = await _context.Transactions.SingleOrDefaultAsync(m => m.ID == id);

                //
                // Save the file to blob storage
                //
                // TODO: Consolodate this with the exact same copy which is in ApiController
                //

                _storage.Initialize();

                string blobname = id.ToString();

                var formFile = files.Single();
                using (var stream = formFile.OpenReadStream())
                {
                    await _storage.UploadToBlob(BlobStoreName, blobname, stream, formFile.ContentType);
                }

                // Save it in the Transaction
                // If there was a problem, UploadToBlob will throw an exception.

                transaction.ReceiptUrl = blobname;
                _context.Update(transaction);
                await _context.SaveChangesAsync();

                return Redirect($"/Transactions/Edit/{id}");
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> ReceiptAction(int id, string action)
        {
            if (action == "delete")
                return await DeleteReceipt(id);
            else if (action == "get")
                return await GetReceipt(id);
            else
                return RedirectToAction(nameof(Edit), new { id });
        }

        private async Task<IActionResult> DeleteReceipt(int id)
        {
            try
            {
                var transaction = await _context.Transactions.Where(x => x.ID == id).SingleAsync();

                transaction.ReceiptUrl = null;
                _context.Update(transaction);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Edit), new { id });
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReceipt(int id)
        {
            try
            {
                if (null == _storage)
                    throw new InvalidOperationException("Unable to download receipt. Azure Blob Storage is not configured for this application.");

                var transaction = await _context.Transactions.SingleOrDefaultAsync(m => m.ID == id);

                if (string.IsNullOrEmpty(transaction.ReceiptUrl))
                    throw new KeyNotFoundException("Transaction has no receipt");

                var blobname = id.ToString();

                // See Bug #991: Production bug: Receipts before 5/20/2021 don't download
                // If the ReceiptUrl contains an int value, use THAT for the blobname instead.

                if (Int32.TryParse(transaction.ReceiptUrl,out _))
                    blobname = transaction.ReceiptUrl;

                _storage.Initialize();
                var stream = new System.IO.MemoryStream();
                var contenttype = await _storage.DownloadBlob(BlobStoreName, blobname, stream);

                // Work around previous versions which did NOT store content type in blob store.
                if ("application/octet-stream" == contenttype)
                    contenttype = "application/pdf";

                stream.Seek(0, System.IO.SeekOrigin.Begin);
                return File(stream, contenttype, id.ToString());
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> UpSplits(List<IFormFile> files, int id)
        {
            try
            {
                if (files == null || !files.Any())
                    throw new ApplicationException("Please choose a file to upload, first.");

                var transaction = await _context.Transactions.Include(x => x.Splits)
                    .SingleAsync(m => m.ID == id);

                var incoming = new HashSet<Models.Split>();
                // Extract submitted file into a list objects

                foreach (var file in files)
                {
                    if (file.FileName.ToLower().EndsWith(".xlsx"))
                    {
                        using var stream = file.OpenReadStream();
                        using var ssr = new SpreadsheetReader();
                        ssr.Open(stream);
                        var items = ssr.Deserialize<Split>(exceptproperties: new string[] { "ID" });
                        incoming.UnionWith(items);
                    }
                }

                if (incoming.Any())
                {
                    // Why no has AddRange??
                    foreach (var split in incoming)
                    {
                        transaction.Splits.Add(split);
                    }

                    _context.Update(transaction);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("Edit", new { id = id });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        private async Task LoadTransactionsFromOfxAsync(IFormFile file, List<Models.Transaction> transactions)
        {
            using var stream = file.OpenReadStream();
            OfxDocument Document = await OfxDocumentReader.FromSgmlFileAsync(stream);

            var created = Document.Statements.SelectMany(x=>x.Transactions).Select(
                tx => new Models.Transaction() 
                {
                    Amount = tx.Amount, 
                    Payee = tx.Memo?.Trim(), 
                    BankReference = tx.ReferenceNumber?.Trim(), 
                    Timestamp = tx.Date.Value.DateTime
                }
            );

            transactions.AddRange(created);
        }

        private void LoadTransactionsFromXlsx(IFormFile file, List<Models.Transaction> transactions, List<IGrouping<int, Models.Split>> splits)
        {
            using var stream = file.OpenReadStream();
            using var ssr = new SpreadsheetReader();
            ssr.Open(stream);
            var items = ssr.Deserialize<Transaction>();
            transactions.AddRange(items);

            // If there are also splits included here, let's grab those
            // And transform the flat data into something easier to use.
            if (ssr.SheetNames.Contains("Split"))
                splits.AddRange(ssr.Deserialize<Split>()?.ToLookup(x => x.TransactionID));
        }

        [HttpPost]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            var highlights = new List<Models.Transaction>();

            //
            // (1) Load file(s) into 'incoming' list
            //

            var incoming = new List<Models.Transaction>();
            var splits = new List<IGrouping<int, Models.Split>>();
            try
            {
                if (files == null || !files.Any())
                    throw new ApplicationException("Please choose a file to upload, first.");

                // Build the submitted file into a list of transactions

                foreach (var formFile in files)
                {
                    var extension = System.IO.Path.GetExtension(formFile.FileName).ToLowerInvariant();
                    if (extension == ".ofx")
                    {
                        await LoadTransactionsFromOfxAsync(formFile,incoming);
                    }
                    else if (extension == ".xlsx")
                    {
                        LoadTransactionsFromXlsx(formFile,incoming,splits);
                    }
                }

                // Process needed changes on each
                foreach (var item in incoming)
                {
                    // Default status for imported items
                    item.Selected = true;
                    item.Imported = true;
                    item.Hidden = true;

                    // Generate a bank reference if doesn't already exist
                    if (string.IsNullOrEmpty(item.BankReference))
                        item.GenerateBankReference();
                }

                //
                // (2) Handle duplicates
                //

                // Deselect duplicate transactions. By default, deselected transactions will not be imported. User can override.

                // To handle the case where there may be transactions already in the system before the importer
                // assigned them a bankreference, we will assign bankreferences retroactively to any overlapping
                // transactions in the system.

                await EnsureAllTransactionsHaveBankRefs();

                // Flag duplicate transactions. If there is an existing transaction with the same bank reference, we'll have to investigate further

                var highlightme = ManageConflictingImports(incoming);
                highlights.AddRange(highlightme);

                //
                // (3) Final processing on each transction
                //

                var payeematcher = new PayeeMatcher(_context);
                await payeematcher.LoadAsync();

                // Process each item


                foreach (var item in incoming)
                {
                    // (3A) Fixup and match payees

                    payeematcher.FixAndMatch(item);

                    // (3B) Import splits
                    // Product Backlog Item 870: Export & import transactions with splits

                    var mysplits = splits.Where(x=>x.Key == item.ID).SelectMany(x=>x);
                    if (mysplits.Any())
                    {
                        item.Splits = mysplits.ToList();
                        item.Category = null;
                        foreach (var split in item.Splits)
                        {
                            // Clear any imported IDs
                            split.ID = 0;
                            split.TransactionID = 0;
                        }
                    }

                    // (3C) Clear any imported ID
                    item.ID = 0;
                }

                // Add resulting transactions

                await _context.AddRangeAsync(incoming);
                await _context.SaveChangesAsync();
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

            // This is kind of a crappy way to communicate the potential false negative conflicts.
            // If user returns to Import page directly, these highlights will be lost. Really probably
            // should persist this to the database somehow. Or at least stick it in the session??
            return RedirectToAction(nameof(Import), new { highlight = string.Join(':', highlights.Select(x => x.ID)) });
        }

        async Task EnsureAllTransactionsHaveBankRefs()
        {
            // To handle the case where there may be transactions already in the system before the importer
            // assigned them a bankreference, we will assign bankreferences retroactively to any overlapping
            // transactions in the system.

            var needbankrefs = _context.Transactions.Where(x => null == x.BankReference);
            if (await needbankrefs.AnyAsync())
            {
                foreach (var tx in needbankrefs)
                {
                    tx.GenerateBankReference();
                }
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Deal with conflicting transactions
        /// </summary>
        /// <remarks>
        /// For each incoming transaction, deselect it if there is already a transaction with a matching bankref in
        /// the database. If the transaction with a matching bankref doesn't exactly equal the considered transaction,
        /// it will be included in the returned tranasactions. These are the suspicious transactions that the user
        /// should look at more carefully.
        /// </remarks>
        IEnumerable<Transaction> ManageConflictingImports(IEnumerable<Transaction> incoming)
        {
            var result = new List<Transaction>();

            // Flag duplicate transactions. If there is an existing transaction with the same bank reference, we'll have to investigate further

            // The approach is to create a lookup, from bankreference to a list of possible matching conflicts. Note that it is possible for multiple different
            // transactions to collide on a single hash. We will have to step through all the possible conflicts to see if there is really a match.

            // Note that this expression evaluates nicely into SQL. Nice job EF peeps!
            /*
                SELECT [x].[ID], [x].[AccountID], [x].[Amount], [x].[BankReference], [x].[Category], [x].[Hidden], [x].[Imported], [x].[Memo], [x].[Payee], [x].[ReceiptUrl], [x].[Selected], [x].[SubCategory], [x].[Timestamp]
                FROM [Transactions] AS [x]
                WHERE [x].[BankReference] IN (N'A1ABC7FE34871F02304982126CAF5C5C', N'EE49717DE89A3D97A9003230734A94B7')
                */
            //
            var uniqueids = incoming.Select(x => x.BankReference).ToHashSet();
            var conflicts = _context.Transactions.Where(x => uniqueids.Contains(x.BankReference)).ToLookup(x => x.BankReference, x => x);

            if (conflicts.Any())
            {
                foreach (var tx in incoming)
                {
                    // If this has any bank ID conflict, we are doing to deselect it. The BY FAR most common case of a
                    // Bankref collision is a duplicate transaction

                    if (conflicts[tx.BankReference].Any())
                    {
                        Console.WriteLine($"{tx.Payee} ({tx.BankReference}) has a conflict");

                        // Deselect the transaction. User will have a chance later to re-select it
                        tx.Selected = false;

                        // That said, there IS a chance that this is honestly a new transaction with a bankref collision.
                        // If we can't find the obvious collision, we'll flag it for the user to sort it out. Still, the
                        // most likely case is it's a legit duplicate but the user made slight changes to the payee or
                        // date.

                        if (!conflicts[tx.BankReference].Any(x => x.Equals(tx)))
                        {
                            Console.WriteLine($"Conflict may be a false positive, flagging for user.");
                            result.Add(tx);
                        }
                    }
                }
            }

            return result;
        }

        // POST: Transactions/Download
        //[ActionName("Download")]
        [HttpPost]
        public async Task<IActionResult> Download(bool allyears, string q = null)
        {
            try
            {
                // Which transactions?

                var transactionsquery = TransactionsForQuery(_context.Transactions.Include(x => x.Splits), q);

                transactionsquery = transactionsquery.Where(x => x.Hidden != true);
                if (!allyears)
                    transactionsquery = transactionsquery.Where(x => x.Timestamp.Year == Year);
                transactionsquery = transactionsquery
                    .OrderByDescending(x => x.Timestamp);

                // Select to data transfer object
                var transactionsdtoquery = transactionsquery
                    .Select(x=> new TransactionExportDto()
                    {
                        ID = x.ID,
                        Amount = x.Amount,
                        Timestamp = x.Timestamp,
                        Category = x.Category,
                        Payee = x.Payee,
                        Memo = x.Memo,
                        ReceiptUrl = x.ReceiptUrl,
                        BankReference = x.BankReference
                    }
                    );

                var transactions = await transactionsdtoquery.ToListAsync();

                // Which splits?

                // Product Backlog Item 870: Export & import transactions with splits
                var splitsquery = _context.Splits.Where(x => x.Transaction.Hidden != true);
                if (!allyears)
                    splitsquery = splitsquery.Where(x => x.Transaction.Timestamp.Year == Year);
                splitsquery = splitsquery.OrderByDescending(x => x.Transaction.Timestamp);
                var splits = await splitsquery.ToListAsync();

                // Create the spreadsheet result

                var stream = new MemoryStream();
                using (var ssw = new SpreadsheetWriter())
                {
                    ssw.Open(stream);
                    ssw.Serialize(transactions,sheetname:nameof(Transaction));

                    if (splits.Any())
                        ssw.Serialize(splits);
                }

                // Return it to caller

                stream.Seek(0, SeekOrigin.Begin);
                return File(stream, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName:"Transactions.xlsx");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // GET: Transactions/Report
        public IActionResult Report([Bind("id,year,month,showmonths,level")] ReportBuilder.Parameters parms)
        {
            try
            {
                if (string.IsNullOrEmpty(parms.id))
                {
                    parms.id = "all";
                }

                if (parms.year.HasValue)
                    Year = parms.year.Value;
                else
                    parms.year = Year;

                if (!parms.month.HasValue)
                {
                    bool iscurrentyear = (Year == Now.Year);

                    // By default, month is the current month when looking at the current year.
                    // When looking at previous years, default is the whole year (december)
                    if (iscurrentyear)
                        parms.month = Now.Month;
                    else
                        parms.month = 12;
                }

                var result = new ReportBuilder(_context).BuildReport(parms);

                ViewData["report"] = parms.id;
                ViewData["month"] = parms.month;
                ViewData["level"] = result.NumLevels;
                ViewData["showmonths"] = result.WithMonthColumns;
                ViewData["Title"] = result.Name;

                return View(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        #endregion

        #region Internals

        private readonly ApplicationDbContext _context;

        private readonly IPlatformAzureStorage _storage;

        private readonly IConfiguration _config;

        private int? _Year = null;
        private int Year
        {
            get
            {
                if (!_Year.HasValue)
                {
                    var value = HttpContext?.Session.GetString(nameof(Year));
                    if (string.IsNullOrEmpty(value))
                    {
                        Year = Now.Year;
                    }
                    else
                    {
                        _Year = (int.TryParse(value, out int y)) ? y : Now.Year;
                    }
                }

                return _Year.Value;
            }
            set
            {
                _Year = value;

                var serialisedDate = _Year.ToString();
                HttpContext?.Session.SetString(nameof(Year), serialisedDate);
            }
        }

        /// <summary>
        /// Current datetime
        /// </summary>
        /// <remarks>
        /// Which may be overridden by tests
        /// </remarks>
        public DateTime Now
        {
            get
            {
                return _Now ?? DateTime.Now;
            }
            set
            {
                _Now = value;
            }
        }
        private DateTime? _Now;

        private string BlobStoreName => _config["Storage:BlobContainerName"] ?? throw new ApplicationException("Must define a blob container name");

        #endregion

        #region IController
        Task<IActionResult> IController<Models.Transaction>.Index() => Index();

        Task<IActionResult> IController<Models.Transaction>.Edit(int id, Models.Transaction item) => Edit(id, false, item);

        Task<IActionResult> IController<Models.Transaction>.Download() => Download(false);
        #endregion

        #region Helpers

        /// <summary>
        /// Set categories for imported transactions based on payee matching rules
        /// </summary>
        class PayeeMatcher
        {
            List<Payee> payees;
            IEnumerable<Payee> regexpayees;

            readonly ApplicationDbContext _mycontext;

            public PayeeMatcher(ApplicationDbContext context)
            {
                _mycontext = context;
            }

            public async Task LoadAsync()
            {
                // Load all payees into memory. This is an optimization. Rather than run a separate payee query for every 
                // transaction, we'll pull it all into memory. This assumes the # of payees is not out of control.

                payees = await _mycontext.Payees.ToListAsync();
                regexpayees = payees.Where(x => x.Name.StartsWith("/") && x.Name.EndsWith("/"));
            }

            public void FixAndMatch(Transaction item)
            {
                item.FixupPayee();

                if (string.IsNullOrEmpty(item.Category))
                {
                    Payee payee = null;

                    // Product Backlog Item 871: Match payee on regex, optionally
                    foreach (var regexpayee in regexpayees)
                    {
                        var regex = new Regex(regexpayee.Name[1..^2]);
                        if (regex.Match(item.Payee).Success)
                        {
                            payee = regexpayee;
                            break;
                        }
                    }

                    if (null == payee)
                    {
                        payee = payees.FirstOrDefault(x => item.Payee.Contains(x.Name));
                    }

                    if (null != payee)
                    {
                        item.Category = payee.Category;
                    }
                }
            }
        };


        #endregion

        #region Data Transfer Objects

        /// <summary>
        /// The transaction data for export
        /// </summary>
        class TransactionExportDto: ICategory
        {
            public int ID { get; set; }
            public DateTime Timestamp { get; set; }
            public string Payee { get; set; }
            public decimal Amount { get; set; }
            public string Category { get; set; }
            public string Memo { get; set; }
            public string BankReference { get; set; }
            public string ReceiptUrl { get; set; }
        }

        #endregion

        #region ViewModels

        public class ReportLinkViewModel
        {
            public string id { get; set; }
            public string Name { get; set; }
        }

        #endregion
    }
}
