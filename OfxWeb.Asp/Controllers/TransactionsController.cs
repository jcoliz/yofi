using Common.AspNetCore;
using Common.NET;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using OfxSharpLib;
using OfxWeb.Asp.Controllers.Reports;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Controllers
{
    [Authorize(Roles = "Verified")]
    public class TransactionsController : Controller, IController<Models.Transaction>
    {
        #region Constructor

        public TransactionsController(ApplicationDbContext context, IPlatformAzureStorage storage)
        {
            _context = context;
            _storage = storage;
        }

        #endregion

        #region Action Handlers

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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

            var result = _context.Transactions.Include(x => x.Splits).AsQueryable<Models.Transaction>();

            if (!string.IsNullOrEmpty(q))
            {
                var split = q.Split(',');

                foreach (var each in split)
                {
                    var token = (each.Length > 2) ? each.ToLowerInvariant().Substring(0, 2) : string.Empty;

                    if (token == "p=")
                    {
                        var term = each.Substring(2);
                        result = result.Where(x => x.Payee.Contains(term));
                    }
                    else if (token == "c=")
                    {
                        var term = each.Substring(2);
                        if (term.ToLowerInvariant() == "[blank]")
                            result = result.Where(x => string.IsNullOrEmpty(x.Category) && !x.Splits.Any());
                        else
                            result = result.Where(x => 
                                x.Category.Contains(term)
                                ||
                                x.Splits.Any(s=>s.Category.Contains(term))
                            );
                    }
                    else if (token == "m=")
                    {
                        var term = each.Substring(2);
                        result = result.Where(x => x.Memo.Contains(term));
                    }
                    else if (token == "y=")
                    {
                        var term = each.Substring(2);
                        int year;
                        if (Int32.TryParse(term, out year))
                            result = result.Where(x => x.Timestamp.Year == year);
                    }
                    else if (token == "r=")
                    {
                        var term = each.Substring(2);
                        if (term == "0")
                            result = result.Where(x => x.ReceiptUrl == null);
                        else if (term == "1")
                            result = result.Where(x => x.ReceiptUrl != null);
                    }
                    else
                        result = result.Where(x => 
                            x.Category.Contains(each) || 
                            x.Memo.Contains(each) || 
                            x.Payee.Contains(each) || 
                            x.Splits.Any(s=>
                                s.Category.Contains(each) ||
                                s.Memo.Contains(each)
                            )
                        );
                }

            }

            //
            // Process VIEW (V) parameters
            //

            ViewData["ViewP"] = v;

            bool showHidden = v?.ToLowerInvariant().Contains("h") == true;
            bool showSelected = v?.ToLowerInvariant().Contains("s") == true;

            ViewData["ShowHidden"] = showHidden;
            ViewData["ShowSelected"] = showSelected;

            if (!showHidden)
                result = result.Where(x => x.Hidden != true);
            //
            // Process ORDER (O) parameters
            //

            const string default_order = "date_desc";
            ViewData["Order"] = (o == default_order) ? null : o;

            if (string.IsNullOrEmpty(o))
                o = default_order;

            ViewData["DateSortParm"] = o == "date_desc" ? "date_asc" : null; /* not "date_desc", which is default */;
            ViewData["PayeeSortParm"] = o == "payee_asc" ? "payee_desc" : "payee_asc";
            ViewData["CategorySortParm"] = o == "category_asc" ? "category_desc" : "category_asc";
            ViewData["AmountSortParm"] = o == "amount_asc" ? "amount_desc" : "amount_asc";
            ViewData["BankReferenceSortParm"] = o == "ref_asc" ? "ref_desc" : "ref_asc";

            switch (o)
            {
                case "amount_asc":
                    result = result.OrderBy(s => s.Amount);
                    break;
                case "amount_desc":
                    result = result.OrderByDescending(s => s.Amount);
                    break;
                case "ref_asc":
                    result = result.OrderBy(s => s.BankReference);
                    break;
                case "ref_desc":
                    result = result.OrderByDescending(s => s.BankReference);
                    break;
                case "payee_asc":
                    result = result.OrderBy(s => s.Payee);
                    break;
                case "payee_desc":
                    result = result.OrderByDescending(s => s.Payee);
                    break;
                case "category_asc":
                    result = result.OrderBy(s => s.Category);
                    break;
                case "category_desc":
                    result = result.OrderByDescending(s => s.Category);
                    break;
                case "date_asc":
                    result = result.OrderBy(s => s.Timestamp).ThenBy(s => s.BankReference);
                    break;
                case "date_desc":
                default:
                    result = result.OrderByDescending(s => s.Timestamp).ThenByDescending(s => s.BankReference);
                    break;
            }

            var count = await result.CountAsync();

            //
            // Process PAGE (P) parameters
            //

            if (!p.HasValue)
                p = 1;
            else
                ViewData["Page"] = p;

            if (count > pagesize)
            {
                result = result.Skip((p.Value - 1) * pagesize).Take(pagesize);

                if (p > 1)
                    ViewData["PreviousPage"] = p.Value - 1;
                else
                    if ((p + 1) * pagesize < count)
                        ViewData["NextNextPage"] = p.Value + 2;

                if (p * pagesize < count)
                    ViewData["NextPage"] = p.Value + 1;
                else
                    if (p > 2)
                        ViewData["PreviousPreviousPage"] = p.Value - 2;

                if (p > 2)
                    ViewData["FirstPage"] = 1;

                if ((p + 1) * pagesize < count)
                    ViewData["LastPage"] = 1 + (count-1) / pagesize;
            }

            return View(await result.AsNoTracking().ToListAsync());
        }

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

            var split = new Split() { Category = transaction.Category, SubCategory = transaction.SubCategory };

            // Calculate the amount based on how much is remaining.

            var currentamount = transaction.Splits.Select(x => x.Amount).Sum();
            var remaining = transaction.Amount - currentamount;
            split.Amount = remaining;

            transaction.Splits.Add(split);

            // Remove the category information, that's now contained in the splits.

            transaction.Category = null;
            transaction.SubCategory = null;

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

        public async Task<IActionResult> Import(string command, string highlight = null)
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
            foreach (var item in _context.Transactions.Where(x=>x.Selected == true))
            {
                item.Selected = false;

                if (!string.IsNullOrEmpty(Category))
                    item.Category = Category;
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

            if (string.IsNullOrEmpty(transaction.Category) && string.IsNullOrEmpty(transaction.SubCategory))
            {
                // See if the payee exists
                var payee = await _context.Payees.FirstOrDefaultAsync(x => transaction.Payee.Contains(x.Name));

                if (payee != null)
                {
                    transaction.Category = payee.Category;
                    transaction.SubCategory = payee.SubCategory;
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

            if (string.IsNullOrEmpty(transaction.Category) && string.IsNullOrEmpty(transaction.SubCategory))
            {
                // See if the payee exists
                var payee = await _context.Payees.FirstOrDefaultAsync(x => transaction.Payee.Contains(x.Name));

                if (payee != null)
                {
                    transaction.Category = payee.Category;
                    transaction.SubCategory = payee.SubCategory;
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
                var transaction = await _context.Transactions.SingleOrDefaultAsync(m => m.ID == id);

                //
                // Save the file to blob storage
                //

                _storage.Initialize();

                string contenttype = null;

                foreach (var formFile in files)
                {
                    using (var stream = formFile.OpenReadStream())
                    {
                        // Upload the file
                        await _storage.UploadToBlob(BlobStoreName, id.ToString(), stream, formFile.ContentType);

                        // Remember the content type
                        // TODO: This can just be a true/false bool, cuz now we store content type in blob store.
                        contenttype = formFile.ContentType;
                    }
                }

                // Save it in the Transaction

                if (null != contenttype)
                {
                    transaction.ReceiptUrl = contenttype;
                    _context.Update(transaction);
                    await _context.SaveChangesAsync();
                }

                return Redirect($"/Transactions/Edit/{id}");
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        // GET: Transactions/DeleteReceipt/5
        public async Task<IActionResult> DeleteReceipt(int? id)
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


        [HttpPost]
        public async Task<IActionResult> UpSplits(List<IFormFile> files, int id)
        {
            try
            {
                var transaction = await _context.Transactions.Include(x => x.Splits)
                    .SingleAsync(m => m.ID == id);

                var incoming = new HashSet<Models.Split>();
                // Extract submitted file into a list objects

                foreach(var file in files)
                {
                    if (file.FileName.ToLower().EndsWith(".xlsx"))
                    {
                        using (var stream = file.OpenReadStream())
                        {
                            var excel = new ExcelPackage(stream);
                            var worksheet = excel.Workbook.Worksheets.First();
                            worksheet.ExtractInto(incoming);
                        }
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
                return BadRequest(ex);
            }
        }


        [HttpPost]
        public async Task<IActionResult> Upload(List<IFormFile> files, string date)
        {
            var highlights = new List<Models.Transaction>();
            var incoming = new List<Models.Transaction>();
            ILookup<int, Models.Split> splits = null;
            try
            {
                // Unless otherwise specified, cut off transactions before
                // 1/1/2020, in case there's a huge file of ancient transactions.
                DateTime cutoff = new DateTime(2020, 01, 01);

                // Check on the date we are sent
                if (!string.IsNullOrEmpty(date))
                {
                    DateTime.TryParse(date, out cutoff);
                }

                // Build the submitted file into a list of transactions

                foreach (var formFile in files)
                {
                    if (formFile.FileName.ToLower().EndsWith(".ofx"))
                    {
                        using (var stream = formFile.OpenReadStream())
                        {
                            var parser = new OfxDocumentParser();
                            var Document = parser.Import(stream);

                            await Task.Run(() =>
                            {
                                foreach (var tx in Document.Transactions)
                                {
                                    var txmodel = new Models.Transaction() { Amount = tx.Amount, Payee = tx.Memo.Trim(), BankReference = tx.ReferenceNumber.Trim(), Timestamp = tx.Date, Selected = true };
                                    if (string.IsNullOrEmpty(txmodel.BankReference))
                                        txmodel.GenerateBankReference();

                                    incoming.Add(txmodel);
                                }
                            });
                        }
                    }
                    else
                    if (formFile.FileName.ToLower().EndsWith(".xlsx"))
                    {
                        using (var stream = formFile.OpenReadStream())
                        {
                            var excel = new ExcelPackage(stream);
                            var worksheet = excel.Workbook.Worksheets.Where(x => x.Name == "Transactions").Single();
                            worksheet.ExtractInto(incoming, includeids: true);

                            // If there are also splits included here, let's grab those
                            worksheet = excel.Workbook.Worksheets.Where(x => x.Name == "Splits").SingleOrDefault();
                            if (null != worksheet)
                            {
                                var flatsplits = new List<Models.Split>();
                                worksheet.ExtractInto(flatsplits,includeids:true);

                                // Transform the flat data into something easier to use.
                                splits = flatsplits.ToLookup(x => x.TransactionID);
                            }
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

                // REmove too early transactions
                if (cutoff > DateTime.MinValue)
                {
                    incoming.RemoveAll(x => x.Timestamp < cutoff);
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

                            if ( ! conflicts[tx.BankReference].Any(x => x.Equals(tx)) )
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
                            var regex = new Regex(regexpayee.Name.Substring(1, regexpayee.Name.Length - 2));
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
                            item.SubCategory = payee.SubCategory;
                        }
                    }

                    // Product Backlog Item 870: Export & import transactions with splits

                    if (splits?.Contains(item.ID) ?? false)
                    {
                        item.Splits = new List<Split>();
                        item.Category = null;
                        item.SubCategory = null;
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
                return BadRequest(ex);
            }

            // This is kind of a crappy way to communicate the potential false negative conflicts.
            // If user returns to Import page directly, these highlights will be lost. Really probably
            // should persist this to the database somehow. Or at least stick it in the session??
            return RedirectToAction(nameof(Import), new { highlight = string.Join(':',highlights.Select(x=>x.ID)) });
        }

        // GET: Transactions/GetReceipt/5
        [ActionName("GetReceipt")]
        public async Task<IActionResult> GetReceipt(int id)
        {
            try
            {
                var transaction = await _context.Transactions.SingleOrDefaultAsync(m => m.ID == id);

                if (string.IsNullOrEmpty(transaction.ReceiptUrl))
                    throw new ApplicationException("Transaction has no receipt");

                _storage.Initialize();
                var stream = new System.IO.MemoryStream();
                var contenttype = await _storage.DownloadBlob(BlobStoreName, id.ToString(), stream);

                // Work around previous versions which did NOT store content type in blob store.
                if ("application/octet-stream" == contenttype)
                    contenttype = "application/pdf";

                stream.Seek(0, System.IO.SeekOrigin.Begin);
                return File(stream, contenttype,id.ToString());
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        // POST: Transactions/Download
        //[ActionName("Download")]
        [HttpPost]
        public async Task<IActionResult> Download(bool allyears, bool mapcheck)
        {
            const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            try
            {
                var objecttype = "Transactions";
                var transactionsquery = _context.Transactions.Where(x => x.Hidden != true);
                if (!allyears)
                    transactionsquery = transactionsquery.Where(x=> x.Timestamp.Year == Year);
                transactionsquery = transactionsquery.OrderByDescending(x => x.Timestamp);
                var transactions = await transactionsquery.ToListAsync();

                CategoryMapper maptable = null;
                if (mapcheck)
                {
                    maptable = new CategoryMapper(_context.CategoryMaps);
                    foreach (var tx in transactions)
                        maptable.MapObject(tx);
                }

                byte[] reportBytes;
                using (var package = new ExcelPackage())
                {
                    package.Workbook.Properties.Title = objecttype;
                    package.Workbook.Properties.Author = "coliz.com";
                    package.Workbook.Properties.Subject = objecttype;
                    package.Workbook.Properties.Keywords = objecttype;

                    var worksheet = package.Workbook.Worksheets.Add(objecttype);
                    int rows, cols;
                    worksheet.PopulateFrom(transactions, out rows, out cols);

                    var tbl = worksheet.Tables.Add(new ExcelAddressBase(fromRow: 1, fromCol: 1, toRow: rows, toColumn: cols), objecttype);
                    tbl.ShowHeader = true;
                    tbl.TableStyle = TableStyles.Dark9;

                    // Product Backlog Item 870: Export & import transactions with splits
                    var splitsquery = _context.Splits.Include(x => x.Transaction).Where(x => x.Transaction.Hidden != true);
                    if (!allyears)
                        splitsquery = splitsquery.Where(x => x.Transaction.Timestamp.Year == Year);
                    splitsquery = splitsquery.OrderByDescending(x => x.Transaction.Timestamp);
                    var splits = await splitsquery.ToListAsync();

                    if (splits.Any())
                    {
                        if (mapcheck)
                            foreach (var split in splits)
                                maptable.MapObject(split);

                        worksheet = package.Workbook.Worksheets.Add("Splits");
                        worksheet.PopulateFrom(splits, out rows, out cols);
                        tbl = worksheet.Tables.Add(new ExcelAddressBase(fromRow: 1, fromCol: 1, toRow: rows, toColumn: cols), "Splits");
                        tbl.ShowHeader = true;
                        tbl.TableStyle = TableStyles.Dark9;
                    }

                    reportBytes = package.GetAsByteArray();
                }

                return File(reportBytes, XlsxContentType, $"{objecttype}.xlsx");
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

            return View(result);
        }

#endregion

#region Internals

        private readonly ApplicationDbContext _context;

        private readonly IPlatformAzureStorage _storage;

        private const int pagesize = 100;

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
                        int y = DateTime.Now.Year;
                        int.TryParse(value, out y);
                        _Year = y;
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

        private string BlobStoreName
        {
            get
            {
                var receiptstore = Environment.GetEnvironmentVariable("RECEIPT_STORE");
                if (string.IsNullOrEmpty(receiptstore))
                    receiptstore = "myfire-undefined";

                return receiptstore;
            }
        }

        private bool TransactionExists(int id)
        {
            return _context.Transactions.Any(e => e.ID == id);
        }

#endregion

#region IController
        Task<IActionResult> IController<Models.Transaction>.Index() => Index();

        Task<IActionResult> IController<Models.Transaction>.Edit(int id, Models.Transaction item) => Edit(id, false, item);

        Task<IActionResult> IController<Models.Transaction>.Upload(List<IFormFile> files) => Upload(files, string.Empty);

        Task<IActionResult> IController<Models.Transaction>.Download() => Download(false, false);
#endregion
    }
}
