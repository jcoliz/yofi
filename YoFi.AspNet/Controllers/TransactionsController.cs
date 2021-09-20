using Common.AspNet;
using Common.NET;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OfxSharp;
using YoFi.AspNet.Controllers.Reports;
using YoFi.AspNet.Data;
using YoFi.AspNet.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoFi.AspNet.Common;
using Transaction = YoFi.AspNet.Models.Transaction;
using System.IO;
using System.ComponentModel.DataAnnotations;
using YoFi.AspNet.Boilerplate.Models;

namespace YoFi.AspNet.Controllers
{
    [Authorize(Roles = "Verified")]
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

            bool showHidden = v?.ToLowerInvariant().Contains("h") == true;
            bool showSelected = v?.ToLowerInvariant().Contains("s") == true;

            ViewData["ShowHidden"] = showHidden;
            ViewData["ShowSelected"] = showSelected;
            ViewData["ToggleHidden"] = (showHidden ? string.Empty : "h") + (showSelected ? "s" : string.Empty);
            ViewData["ToggleSelected"] = (showHidden ? "h" : string.Empty) + (showSelected ? string.Empty : "s"); ;

            if (!showHidden)
                result = result.Where(x => x.Hidden != true);

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

            result = o switch 
            { 
                "aa" => result.OrderBy(s => s.Amount),
                "ad" => result.OrderByDescending(s => s.Amount), // "amount_desc":
                "ra" => result.OrderBy(s => s.BankReference), // "ref_asc":
                "rd" => result.OrderByDescending(s => s.BankReference), // "ref_desc":
                "pa" => result.OrderBy(s => s.Payee), // "payee_asc":
                "pd" => result.OrderByDescending(s => s.Payee), // "payee_desc":
                "ca" => result.OrderBy(s => s.Category), // "category_asc":
                "cd" => result.OrderByDescending(s => s.Category),// "category_desc":
                "da" => result.OrderBy(s => s.Timestamp).ThenBy(s => s.BankReference), // "date_asc":
                 _ => result.OrderByDescending(s => s.Timestamp).ThenByDescending(s => s.BankReference) // "date_desc":
            };

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
        public async Task<IActionResult> CreateSplit(int id)
        {
            /*
				1) ADD a split to the transaction in the full amount needed to get back to total amount.
				2) COPY the Category Information over from the main, if exists
                3) NULL out the Cat/SubCat from the transaction
             */
            var transaction = await _context.Transactions.Include("Splits")
                .SingleOrDefaultAsync(m => m.ID == id);
            if (transaction == null)
            {
                return NotFound();
            }

            var split = new Split() { Category = transaction.Category };

            // Calculate the amount based on how much is remaining.

            var currentamount = transaction.Splits.Select(x => x.Amount).Sum();
            var remaining = transaction.Amount - currentamount;
            split.Amount = remaining;

            transaction.Splits.Add(split);

            // Remove the category information, that's now contained in the splits.

            transaction.Category = null;

            _context.Update(transaction);
            await _context.SaveChangesAsync();

            return RedirectToAction("Edit", "Splits", new { id = split.ID });
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
                .SingleOrDefaultAsync(m => m.ID == id);
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessImported(string command)
        {
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
            }
            else if (command == "ok")
            {
                foreach (var item in selected)
                    item.Imported = item.Hidden = item.Selected = false;
                _context.Transactions.RemoveRange(unselected);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Import));
        }

        public async Task<IActionResult> Import(string highlight = null)
        {
            var allimported = from s in _context.Transactions
                              where s.Imported == true
                              orderby s.Timestamp descending, s.BankReference ascending
                              select s;

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

            return View(await allimported.AsNoTracking().ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> BulkEdit(string Category)
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


        public IActionResult Create()
        {
            return View();
        }

        // POST: Transactions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Timestamp,Amount,Memo,Payee,Category,SubCategory,BankReference")] Models.Transaction transaction)
        {
            if (ModelState.IsValid)
            {
                _context.Add(transaction);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(transaction);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions.Include(x => x.Splits).SingleOrDefaultAsync(m => m.ID == id);
            if (transaction == null)
            {
                return NotFound();
            }

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

        public async Task<IActionResult> EditModal(int? id)
        {
            // TODO: Refactor to no duplicate between here and Edit
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions.Include(x => x.Splits).SingleOrDefaultAsync(m => m.ID == id);
            if (transaction == null)
            {
                return NotFound();
            }

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
        public async Task<IActionResult> Edit(int id, bool? duplicate, [Bind("ID,Timestamp,Amount,Memo,Payee,Category,SubCategory,BankReference")] Models.Transaction transaction)
        {
            if (id != transaction.ID && duplicate != true)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
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
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TransactionExists(transaction.ID))
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
            return View(transaction);
        }

        // GET: Transactions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
                .SingleOrDefaultAsync(m => m.ID == id);
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        // POST: Transactions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transaction = await _context.Transactions.SingleOrDefaultAsync(m => m.ID == id);
            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
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
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiptAction(int id, string action)
        {
            if (action == "delete")
                return await DeleteReceipt(id);
            else if (action == "get")
                return await GetReceipt(id);
            else
                return RedirectToAction(nameof(Edit), new { id });
        }

        private async Task<IActionResult> DeleteReceipt(int? id)
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

            transaction.ReceiptUrl = null;
            _context.Update(transaction);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Edit), new { id });
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
                    throw new ApplicationException("Transaction has no receipt");

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
            catch (Exception ex)
            {
                return Problem(detail:ex.Message,type:ex.GetType().Name);
            }
        }

        [HttpPost]
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
                        using var ssr = new OpenXmlSpreadsheetReader();
                        ssr.Open(stream);
                        var items = ssr.Read<Split>(exceptproperties: new string[] { "ID" });
                        incoming.UnionWith(items);
                    }
                }

                if (incoming.Any())
                {
                    // Why no has AddRange??
                    foreach (var split in incoming)
                    {
                        split.FixupCategories();
                        transaction.Splits.Add(split);
                    }

                    _context.Update(transaction);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("Edit", new { id = id });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            var highlights = new List<Models.Transaction>();
            var incoming = new List<Models.Transaction>();
            ILookup<int, Models.Split> splits = null;
            try
            {
                if (files == null || !files.Any())
                    throw new ApplicationException("Please choose a file to upload, first.");

                // Build the submitted file into a list of transactions

                foreach (var formFile in files)
                {
                    if (formFile.FileName.ToLower().EndsWith(".ofx"))
                    {
                        using var stream = formFile.OpenReadStream();
                        OfxDocument Document = await OfxDocumentReader.FromSgmlFileAsync(stream);
                            
                        await Task.Run(() =>
                        {
                            foreach (var tx in Document.Statements.SelectMany(x=>x.Transactions))
                            {
                                var txmodel = new Models.Transaction() { Amount = tx.Amount, Payee = tx.Memo?.Trim(), BankReference = tx.ReferenceNumber?.Trim(), Timestamp = tx.Date.Value.DateTime, Selected = true };
                                if (string.IsNullOrEmpty(txmodel.BankReference))
                                    txmodel.GenerateBankReference();

                                incoming.Add(txmodel);
                            }
                        });
                    }
                    else
                    if (formFile.FileName.ToLower().EndsWith(".xlsx"))
                    {
                        using (var stream = formFile.OpenReadStream())
                        using (var ssr = new OpenXmlSpreadsheetReader())
                        {
                            ssr.Open(stream);
                            var items = ssr.Read<Transaction>();
                            incoming.AddRange(items);

                            // If there are also splits included here, let's grab those
                            // And transform the flat data into something easier to use.
                            if (ssr.SheetNames.Contains("Split"))
                                splits = ssr.Read<Split>()?.ToLookup(x => x.TransactionID);
                        }

                        // Need to select all of these, so they import by default
                        foreach (var import in incoming)
                        {
                            import.Selected = true;
                            if (string.IsNullOrEmpty(import.BankReference))
                                import.GenerateBankReference();
                        }
                    }
                }

                // Deselect duplicate transactions. By default, deselected transactions will not be imported. User can override.

                // Flag duplicate transactions. If there is an existing transaction with the same bank reference, we'll have to investigate further
                var uniqueids = incoming.Select(x => x.BankReference).ToHashSet();

                /*
                 * Rethinking this. If you later move a transaction to a new date, it will probably not be caught
                 * by this check, because we are narrowing to only the imported dates.
                 * 

                // This is the set of transactions which overlap with the timeframe of the imported transactions.
                // By definitions, all duplicate transactions (conflicts) will be in this set.
                var mindate = incoming.Min(x => x.Timestamp);
                var maxdate = incoming.Max(x => x.Timestamp);
                var conflictrange = _context.Transactions.Where(x => x.Timestamp >= mindate && x.Timestamp <= maxdate);
                */

                var conflictrange = _context.Transactions;

                // To handle the case where there may be transactions already in the system before the importer
                // assigned them a bankreference, we will assign bankreferences retroactively to any overlapping
                // transactions in the system.

                var needbankrefs = conflictrange.Where(x => null == x.BankReference);
                if (await needbankrefs.AnyAsync())
                {
                    foreach (var tx in needbankrefs)
                    {
                        tx.GenerateBankReference();
                    }
                    await _context.SaveChangesAsync();
                }

                // The approach is to create a lookup, from bankreference to a list of possible matching conflicts. Note that it is possible for multiple different
                // transactions to collide on a single hash. We will have to step through all the possible conflicts to see if there is really a match.

                // Note that this expression evaluates nicely into SQL. Nice job EF peeps!
                /*
                    SELECT [x].[ID], [x].[AccountID], [x].[Amount], [x].[BankReference], [x].[Category], [x].[Hidden], [x].[Imported], [x].[Memo], [x].[Payee], [x].[ReceiptUrl], [x].[Selected], [x].[SubCategory], [x].[Timestamp]
                    FROM [Transactions] AS [x]
                    WHERE [x].[BankReference] IN (N'A1ABC7FE34871F02304982126CAF5C5C', N'EE49717DE89A3D97A9003230734A94B7')
                 */
                //
                var conflicts = conflictrange.Where(x => uniqueids.Contains(x.BankReference)).ToLookup(x => x.BankReference, x => x);

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
                                highlights.Add(tx);
                            }
                        }
                    }
                }

                // Load all categories into memory. This is an optimization. Rather than run a separate payee query for every 
                // transaction, we'll pull it all into memory. This assumes the # of payees is not out of control.

                var payees = await _context.Payees.ToListAsync();
                var regexpayees = payees.Where(x => x.Name.StartsWith("/") && x.Name.EndsWith("/"));

                // Process each item

                foreach (var item in incoming)
                {
                    item.FixupPayee();
                    item.Imported = true;
                    item.Hidden = true;

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

                    // Product Backlog Item 870: Export & import transactions with splits

                    if (splits?.Contains(item.ID) ?? false)
                    {
                        item.Splits = new List<Split>();
                        item.Category = null;
                        foreach (var split in splits[item.ID])
                        {
                            split.ID = 0;
                            split.TransactionID = 0;
                            item.Splits.Add(split);
                        }
                    }

                    // Clear any imported ID
                    item.ID = 0;
                }

                // Add resulting transactions

                await _context.AddRangeAsync(incoming);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            // This is kind of a crappy way to communicate the potential false negative conflicts.
            // If user returns to Import page directly, these highlights will be lost. Really probably
            // should persist this to the database somehow. Or at least stick it in the session??
            return RedirectToAction(nameof(Import), new { highlight = string.Join(':', highlights.Select(x => x.ID)) });
        }

        // POST: Transactions/Download
        //[ActionName("Download")]
        [HttpPost]
        public async Task<IActionResult> Download(bool allyears, bool mapcheck, string q = null)
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

                // Map categories, if requested

                if (mapcheck)
                {
                    var maptable = new CategoryMapper(_context.CategoryMaps);
                    foreach (var tx in transactions)
                        maptable.MapObject(tx);

                    if (splits.Any())
                        foreach (var split in splits)
                            maptable.MapObject(split);
                }

                // Create the spreadsheet result

                var stream = new MemoryStream();
                using (var ssw = new OpenXmlSpreadsheetWriter())
                {
                    ssw.Open(stream);
                    ssw.Write(transactions,sheetname:nameof(Transaction));

                    if (splits.Any())
                        ssw.Write(splits);
                }

                // Return it to caller

                stream.Seek(0, SeekOrigin.Begin);
                return File(stream, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName:"Transactions.xlsx");
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        // GET: Transactions/Report
        public IActionResult Report([Bind("id,year,month,showmonths,level")] ReportBuilder.Parameters parms)
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
                bool iscurrentyear = (Year == DateTime.Now.Year);

                // By default, month is the current month when looking at the current year.
                // When looking at previous years, default is the whole year (december)
                if (iscurrentyear)
                    parms.month = DateTime.Now.Month;
                else
                    parms.month = 12;
            }

            var result = new ReportBuilder(_context).BuildReport(parms);

            ViewData["report"] = parms.id;
            ViewData["month"] = parms.month;
            ViewData["level"] = result.NumLevels;
            ViewData["showmonths"] = result.WithMonthColumns;
            ViewData["Title"] = result.Name;

            ViewData["AvailableReports"] = ReportBuilder.Definitions.Select(x => new ReportLinkViewModel() { id = x.id, Name = x.Name }).ToList();

            return View(result);
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
                        Year = DateTime.Now.Year;
                    }
                    else
                    {
#pragma warning disable IDE0059 // Unnecessary assignment of a value
                        int y = DateTime.Now.Year;
                        int.TryParse(value, out y);
                        _Year = y;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
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

        private string BlobStoreName => _config["Storage:BlobContainerName"] ?? throw new ApplicationException("Must define a blob container name");

        private bool TransactionExists(int id)
        {
            return _context.Transactions.Any(e => e.ID == id);
        }
        #endregion

        #region IController
        Task<IActionResult> IController<Models.Transaction>.Index() => Index();

        Task<IActionResult> IController<Models.Transaction>.Edit(int id, Models.Transaction item) => Edit(id, false, item);

        Task<IActionResult> IController<Models.Transaction>.Download() => Download(false, false);
        #endregion

        #region Data Transfer Objects

        /// <summary>
        /// The transaction data for export
        /// </summary>
        class TransactionExportDto: ICatSubcat
        {
            public int ID { get; set; }
            public DateTime Timestamp { get; set; }
            public string Payee { get; set; }
            public decimal Amount { get; set; }
            public string Category { get; set; }
            public string Memo { get; set; }
            public string BankReference { get; set; }
            public string ReceiptUrl { get; set; }
            string ICatSubcat.SubCategory { get => null; set { } }
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
