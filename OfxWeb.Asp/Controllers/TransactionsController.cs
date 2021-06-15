using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using OfxWeb.Asp.Controllers.Helpers;
using Microsoft.AspNetCore.Http;
using OfxSharpLib;
using Microsoft.AspNetCore.Authorization;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using ManiaLabs.Portable.Base;
using ManiaLabs.NET;
using System.Web;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using Common.AspNetCore;

namespace OfxWeb.Asp.Controllers
{
    [Authorize(Roles = "Verified")]
    public class TransactionsController : Controller, IController<Models.Transaction>
    {
        private readonly ApplicationDbContext _context;

        private const int pagesize = 100;

        public TransactionsController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;

            var StorageAccount = configuration?["StorageAccount"];
            Console.WriteLine(StorageAccount);

        }

        // GET: Transactions
        public async Task<IActionResult> Index(string sortOrder, string search, string searchPayee, string searchCategory, int? page)
        {
            // Sort/Filter: https://docs.microsoft.com/en-us/aspnet/core/data/ef-mvc/sort-filter-page?view=aspnetcore-2.1

            if (string.IsNullOrEmpty(sortOrder))
                sortOrder = "date_desc";

            ViewData["DateSortParm"] = sortOrder == "date_desc" ? "date_asc" : "date_desc";
            ViewData["PayeeSortParm"] = sortOrder == "payee_asc" ? "payee_desc" : "payee_asc";
            ViewData["CategorySortParm"] = sortOrder == "category_asc" ? "category_desc" : "category_asc";
            ViewData["AmountSortParm"] = sortOrder == "category_asc" ? "category_desc" : "category_asc";
            ViewData["BankReferenceSortParm"] = sortOrder == "ref_asc" ? "ref_desc" : "ref_asc";

            bool showHidden = false;
            bool showSelected = false;
            bool? showHasReceipt = null;
            int? filteryear = null;

            bool didfilter;

            do
            {
                didfilter = false;

                // Pull receipt filtering out of payee
                if (!string.IsNullOrEmpty(searchPayee))
                {
                    if (searchPayee.EndsWith("+R"))
                    {
                        showHasReceipt = true;
                        didfilter = true;
                    }
                    else if (searchPayee.EndsWith("-R"))
                    {
                        showHasReceipt = false;
                        didfilter = true;
                    }
                    if (showHasReceipt.HasValue)
                    {
                        searchPayee = new string(searchPayee.SkipLast(2).ToArray());
                    }
                }

                // Pull year filtering out of payee
                if (!string.IsNullOrEmpty(searchPayee))
                {
                    var re = new Regex("Y(\\d{4})");
                    var match = re.Match(searchPayee);
                    if (match.Success)
                    {
                        filteryear = int.Parse(match.Groups[1].Value);
                        didfilter = true;
                        searchPayee = searchPayee.Remove(match.Index, match.Length);
                    }
                }
            }
            while (didfilter);

            // 'search' parameter combines all search types
            if (!String.IsNullOrEmpty(search))
            {
                var terms = search.Split(',');
                foreach (var term in terms)
                {
                    if (term[0] == 'P')
                    {
                        searchPayee = term.Substring(2);
                    }
                    else if (term[0] == 'C')
                    {
                        searchCategory = term.Substring(2);
                    }
                    else if (term[0] == 'H')
                    {
                        showHidden = (term[1] == '+');
                    }
                    else if (term[0] == 'Z')
                    {
                        showSelected = (term[1] == '+');
                    }
                    else if (term[0] == 'R')
                    {
                        showHasReceipt = (term[1] == '+');
                    }
                    else if (term[0] == 'Y')
                    {
                        var termyear = new string(term.Skip(1).ToArray());
                        int yearval = 0;
                        if (int.TryParse(termyear,out yearval))
                        {
                            filteryear = yearval;
                        }
                    }
                }
            }

            ViewData["ShowHidden"] = showHidden;
            ViewData["ShowSelected"] = showSelected;
            ViewData["CurrentSearchPayee"] = searchPayee;
            ViewData["CurrentSearchCategory"] = searchCategory;

            var searchlist = new List<string>();
            if (!String.IsNullOrEmpty(searchPayee))
                searchlist.Add($"P-{searchPayee}");
            if (!String.IsNullOrEmpty(searchCategory))
                searchlist.Add($"C-{searchCategory}");
            if (showHasReceipt.HasValue)
                searchlist.Add($"R{(showHasReceipt.Value ? '+' : '-')}");
            if (showHidden)
                searchlist.Add("H+");
            if (showSelected)
                searchlist.Add("Z+");
            if (filteryear.HasValue)
                searchlist.Add($"Y{filteryear.Value}");

            ViewData["CurrentFilter"] = string.Join(',', searchlist);

            var togglehiddensearchlist = new List<string>(searchlist);
            togglehiddensearchlist.Add($"H{(showHidden ? '-' : '+')}");
            ViewData["CurrentFilterToggleHidden"] = string.Join(',', togglehiddensearchlist);

            var toggleselectedsearchlist = new List<string>(searchlist);
            toggleselectedsearchlist.Add($"Z{(showSelected ? '-' : '+')}");
            ViewData["CurrentFilterToggleSelected"] = string.Join(',', toggleselectedsearchlist);

            if (!page.HasValue)
                page = 1;

            var result = _context.Transactions.Include(x => x.Splits).AsQueryable<Models.Transaction>();

            if (!String.IsNullOrEmpty(searchPayee))
            {
                result = result.Where(x => x.Payee.Contains(searchPayee));
            }

            if (!String.IsNullOrEmpty(searchCategory))
            {
                if (searchCategory == "-")
                    result = result.Where(x => string.IsNullOrEmpty(x.Category) && !x.Splits.Any());
                else
                    result = result.Where(x => 
                        (x.Category != null && x.Category.Contains(searchCategory)) 
                        || 
                        (x.SubCategory != null && x.SubCategory.Contains(searchCategory))
                        ||
                        (
                            x.Splits.Any(s=> 
                                (s.Category != null && s.Category.Contains(searchCategory))
                                ||
                                (s.SubCategory != null && s.SubCategory.Contains(searchCategory))
                            )
                        )
                    );
            }

            if (!showHidden)
            {
                result = result.Where(x => x.Hidden != true);
            }

            if (showHasReceipt.HasValue)
            {
                if (showHasReceipt.Value)
                    result = result.Where(x => x.ReceiptUrl != null);
                else
                    result = result.Where(x => x.ReceiptUrl == null);
            }

            if (filteryear.HasValue)
            {
                result = result.Where(x => x.Timestamp.Year == filteryear.Value);
            }

            switch (sortOrder)
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

            if (count > pagesize)
            {
                result = result.Skip((page.Value - 1) * pagesize).Take(pagesize);

                if (page.Value > 1)
                    ViewData["PreviousPage"] = page.Value - 1;
                if (page * pagesize < count)
                    ViewData["NextPage"] = page.Value + 1;

                ViewData["CurrentSort"] = sortOrder;
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

        // GET: Transactions/Details/5
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

        // GET: Transactions/Import
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

        // POST: Transactions/BulkEdit
        [HttpPost]
        public async Task<IActionResult> BulkEdit(string Category, string SubCategory)
        {
            var result = from s in _context.Transactions
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
                        item.SubCategory = null;
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


        // GET: Transactions/Create
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

        // GET: Transactions/Edit/5
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

        // GET: Transactions/EditModal/5
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

        // GET: Transactions/ApplyPayee/5
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

                IPlatformAzureStorage storage = new DotNetAzureStorage("DefaultEndpointsProtocol=http;AccountName=jcolizstorage;AccountKey=kjfiUJrgAq/FP0ZL3uVR9c5LPq5dI3MCfCNNnwFRDtrYs63FU654j4mBa4tmkLm331I4Xd/fhZgORnhkEfb4Eg==");
                storage.Initialize();

                string contenttype = null;

                foreach (var formFile in files)
                {
                    using (var stream = formFile.OpenReadStream())
                    {
                        // Upload the file
                        await storage.UploadToBlob(BlobStoreName, id.ToString(), stream, formFile.ContentType);

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

                IPlatformAzureStorage storage = new DotNetAzureStorage("DefaultEndpointsProtocol=http;AccountName=jcolizstorage;AccountKey=kjfiUJrgAq/FP0ZL3uVR9c5LPq5dI3MCfCNNnwFRDtrYs63FU654j4mBa4tmkLm331I4Xd/fhZgORnhkEfb4Eg==");
                storage.Initialize();
                var stream = new System.IO.MemoryStream();
                var contenttype = await storage.DownloadBlob(BlobStoreName, id.ToString(), stream);

                // Work around previous versions which did NOT store content type in blob store.
                if ("application/octet-stream" == contenttype)
                    contenttype = "application/pdf";

                stream.Seek(0, System.IO.SeekOrigin.Begin);
                return File(stream, contenttype);
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

        private IActionResult DownloadReport(Table<Label, Label, decimal> report, string title)
        {
            const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            try
            {
                byte[] reportBytes;
                using (var package = new ExcelPackage())
                {
                    package.Workbook.Properties.Title = title;
                    package.Workbook.Properties.Author = "coliz.com";
                    package.Workbook.Properties.Subject = title;
                    package.Workbook.Properties.Keywords = title;

                    var worksheet = package.Workbook.Worksheets.Add(title);
                    int rows, cols;
                    ExportRawReportTo(worksheet, report, out rows, out cols);

                    var tablename = System.Text.RegularExpressions.Regex.Replace(title, @"\s+", "");
                    var tbl = worksheet.Tables.Add(new ExcelAddressBase(fromRow: 1, fromCol: 1, toRow: rows, toColumn: cols), tablename);
                    tbl.ShowHeader = true;
                    tbl.TableStyle = TableStyles.Dark9;

                    reportBytes = package.GetAsByteArray();
                }

                return File(reportBytes, XlsxContentType, $"{title}.xlsx");
            }
            catch (Exception)
            {
                return NotFound();
            }
        }
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

        // GET: Transactions/Report
        public async Task<IActionResult> Report(string report, int? month, int? weekspct, int? setyear, bool? download)
        {
            Report result = new Report();

            if (string.IsNullOrEmpty(report))
            {
                report = "all";
            }

            if (setyear.HasValue)
                Year = setyear.Value;

            if (!month.HasValue)
            {
                bool iscurrentyear = (Year == DateTime.Now.Year);

                // By default, month is the current month when looking at the current year.
                // When looking at previous years, default is the whole year (december)
                if (iscurrentyear)
                    month = DateTime.Now.Month;
                else
                    month = 12;
            }

            var period = new DateTime(Year, month.Value, 1);

            result.Description = $"For {Year} through {period.ToString("MMMM")} ";
            ViewData["report"] = report;
            ViewData["month"] = month;

            Func<Models.Transaction, bool> inscope_t = (x => x.Timestamp.Year == Year && x.Hidden != true && x.Timestamp.Month <= month);
            Func<Models.Split, bool> inscope_s = (x => x.Transaction.Timestamp.Year == Year && x.Transaction.Hidden != true && x.Transaction.Timestamp.Month <= month);

            var txs = _context.Transactions.Include(x => x.Splits).Where(inscope_t).Where(x => !x.Splits.Any()); 
            var splits = _context.Splits.Include(x => x.Transaction).Where(inscope_s); 

            if (report == "all")
            {
                result.WithMonthColumns = true;
                result.NumLevels = 4;
                result.SingleSource = txs.AsQueryable<IReportable>().Concat(splits);
                result.Name = "All Transactions";
            }
            else if (report == "income")
            {
                var txsI = txs.Where(x => x.Category == "Income" || x.Category.StartsWith("Income:"));
                var splitsI = splits.Where(x => x.Category == "Income" || x.Category.StartsWith("Income:"));

                result.SingleSource = txsI.AsQueryable<IReportable>().Concat(splitsI);
                result.FromLevel = 1;
                result.Name = "Income";
            }

            result.Build();
            result.WriteToConsole();

            return View(result);
        }

        // GET: Transactions/Pivot
        public async Task<IActionResult> Pivot(string report, int? month, int? weekspct, int? setyear, bool? download)
        {
            Table<Label, Label, decimal> result = null;

            if (string.IsNullOrEmpty(report))
            {
                report = "all";
            }

            if (setyear.HasValue)
                Year = setyear.Value;

            if (!month.HasValue)
            {
                bool iscurrentyear = (Year == DateTime.Now.Year);

                // By default, month is the current month when looking at the current year.
                // When looking at previous years, default is the whole year (december)
                if (iscurrentyear)
                    month = DateTime.Now.Month;
                else
                    month = 12;

                // By default, budgetmo goes through LAST month, unless it's January
                if (report == "budgetmo" && month > 1 && iscurrentyear)
                    --month;
            }

            var period = new DateTime(Year, month.Value, 1);

            ViewData["Subtitle"] = $"For {Year} through {period.ToString("MMMM")} ";
            ViewData["report"] = report;
            ViewData["month"] = month;

            var builder = new Helpers.ReportBuilder(_context);
            IEnumerable<IGrouping<int, IReportable>> groupsL1 = null;
            IEnumerable<IGrouping<int, ISubReportable>> groupsL2 = null;
            IQueryable<ISubReportable> txs, splits, combined;
            Func<Models.Transaction, bool> inscope = (x => x.Timestamp.Year == Year && x.Hidden != true && x.Timestamp.Month <= month);

            switch (report)
            {
                case "yearly":
                    groupsL2 = _context.Transactions.Where(x => YearlyCategories.Contains(x.Category)).Where(inscope).GroupBy(x => x.Timestamp.Month);
                    result = await builder.ThreeLevelReport(groupsL2);
                    ViewData["Title"] = "Yearly Report";
                    break;

                case "details":
                    groupsL2 = _context.Transactions.Where(x => DetailCategories.Contains(x.Category)).Where(inscope).GroupBy(x => x.Timestamp.Month);
                    result = await builder.ThreeLevelReport(groupsL2);
                    ViewData["Title"] = "Transaction Details Report";
                    break;

                case "all":
                    txs = _context.Transactions.Include(x => x.Splits).Where(inscope).Where(x => !x.Splits.Any()).AsQueryable<ISubReportable>();
                    splits = _context.Splits.Include(x => x.Transaction).Where(x => inscope(x.Transaction)).AsQueryable<ISubReportable>();
                    combined = txs.Concat(splits);
                    groupsL2 = combined.OrderBy(x => x.Timestamp).GroupBy(x => x.Timestamp.Month);
                    result = await builder.ThreeLevelReport(groupsL2,true);
                    ViewData["Title"] = "Transaction Summary";
                    ViewData["Mapping"] = true;
                    break;

                case "mapped":
                    txs = _context.Transactions.Include(x => x.Splits).Where(inscope).Where(x => !x.Splits.Any()).AsQueryable<ISubReportable>();
                    splits = _context.Splits.Include(x => x.Transaction).Where(x => inscope(x.Transaction)).AsQueryable<ISubReportable>();
                    combined = txs.Concat(splits);
                    groupsL2 = combined.OrderBy(x => x.Timestamp).GroupBy(x => x.Timestamp.Month);
                    result = await builder.FourLevelReport(groupsL2);
                    ViewData["Title"] = "Transaction Summary";
                    ViewData["Mapping"] = true;
                    break;

                case "budgettx":
                    groupsL1 = _context.BudgetTxs.Where(x => x.Timestamp.Year == Year && x.Timestamp.Month <= month).GroupBy(x => x.Timestamp.Month);
                    result = TwoLevelReport(groupsL1);
                    ViewData["Title"] = "Budget Transaction Report";
                    break;

                case "budgetmo":
                    result = BudgetMonthlyReport(month.Value);
                    ViewData["Title"] = "Monthly Budget Report";
                    ViewData["Subtitle"] = $"For {Year} through {period.ToString("MMMM")} ";
                    break;

                case "budget":
                    var labelcol = new Label() { Order = month.Value, Value = new DateTime(Year, month.Value, 1).ToString("MMM") };
                    result = BudgetReport(labelcol, weekspct);
                    ViewData["Title"] = "Budget vs Actuals Report";
                    ViewData["Subtitle"] = $"For {period.ToString("MMM yyyy")}";
                    if (weekspct.HasValue)
                        ViewData["Subtitle"] += $" ({weekspct.Value}%)";
                    break;

                case "monthly":
                    groupsL1 = _context.Transactions.Where(x => (!YearlyCategories.Contains(x.Category) || x.Category == null)).Where(inscope).GroupBy(x => x.Timestamp.Month);
                    result = TwoLevelReport(groupsL1);
                    ViewData["Title"] = "Monthly Report";
                    break;
            }

            if (download == true)
            {
                return DownloadReport(result, (string)ViewData["Title"]);
            }
            else
            {
                return View(result);
            }
        }

        private string[] YearlyCategories = new[] { "RV", "Yearly", "Travel", "Transfer", "Medical", "Pets", "App Development", "Yearly.Housing", "Yearly.James", "Yearly.Sheila", "Yearly.Transportation", "Yearly.Auto & Transport", "Yearly.Entertainment", "Yearly.Kids", "Yearly.Shopping", "Yearly.Entertainment", "Yearly.Utilities" };

        private string[] DetailCategories = new[] { "Auto & Transport", "Transportation", "Groceries", "Utilities", "Entertainment", "Kids", "Housing.Services", "Shopping" };

        private string[] BudgetFocusCategories = new[] { "Entertainment", "Food & Dining", "Dining Out", "Groceries", "Kids", "Shopping" };

        private Table<Label, Label, decimal> BudgetReport(Label month, int? weekspct = null)
        {
            var result = new Table<Label, Label, decimal>();

            IEnumerable<IGrouping<int, IReportable>> groupsL1 = null;

            groupsL1 = _context.BudgetTxs.Where(x => x.Timestamp.Year == Year).GroupBy(x => x.Timestamp.Month);
            var budgettx = TwoLevelReport(groupsL1);

            groupsL1 = _context.Transactions.Where(x => x.Timestamp.Year == Year && BudgetFocusCategories.Contains(x.Category) && x.Hidden != true).GroupBy(x => x.Timestamp.Month);
            var monthlth = TwoLevelReport(groupsL1);

            Label spentLabel = new Label() { Order = 1, Value = "Spent", Format = "C0" };
            Label budgetLabel = new Label() { Order = 2, Value = "Budget", Format = "C0" };
            Label remainingLabel = new Label() { Order = 3, Value = "Remaining", Format = "C0" };
            Label pctSpentLabel = new Label() { Order = 4, Value = "% Spent", Format = "P0" };
            Label pctStatusLabel = new Label() { Order = 5, Value = "Status", Format = "P-warning" };

            foreach (var category in BudgetFocusCategories)
            {
                var labelrow = new Label() { Order = 0, Value = category };

                if (monthlth.RowLabels.Where(x=>x == labelrow).Any())
                {
                    var budgetval = -budgettx[month, labelrow];
                    var spentval = -monthlth[month, labelrow];
                    var remaining = budgetval - spentval;

                    result[spentLabel, labelrow] = spentval;
                    result[budgetLabel, labelrow] = budgetval;
                    result[remainingLabel, labelrow] = remaining;

                    if (budgetval > 0)
                    {
                        var pct = spentval / budgetval;
                        result[pctSpentLabel, labelrow] = pct;

                        if (weekspct.HasValue)
                        {
                            var pctStatus = pct / (weekspct.Value / 100.0M);
                            result[pctStatusLabel, labelrow] = pctStatus;
                        }
                    }
                }
            }

            return result;
        }

        private Table<Label, Label, decimal> BudgetMonthlyReport(int monththrough)
        {
            var result = new Table<Label, Label, decimal>();

            IEnumerable<IGrouping<int, IReportable>> groupsL1 = null;

            var budgettxquery = _context.BudgetTxs.Where(x => x.Timestamp.Year == Year && x.Timestamp.Month <= monththrough);
            groupsL1 = budgettxquery.GroupBy(x => x.Timestamp.Month);
            var budgettx = TwoLevelReport(groupsL1);

            var categories = budgettxquery.Select(x => x.Category).Distinct().ToHashSet();
            groupsL1 = _context.Transactions.Where(x => x.Timestamp.Year == Year && x.Timestamp.Month <= monththrough && categories.Contains(x.Category) && x.Hidden != true).GroupBy(x => x.Timestamp.Month);
            var monthlth = TwoLevelReport(groupsL1);

            foreach (var row in budgettx.RowLabels)
            {
                var labelrowbudget = new Label() { Order = row.Order + (row.Order > 0 ? 2 : 0), Value = row.Value, SubValue = "Budget" };
                var labelrowactual = new Label() { Order = row.Order + (row.Order > 0 ? 1 : 0), Value = row.Value, SubValue = "Actual" };
                var labelrow = new Label() { Order = row.Order, Value = row.Value, Emphasis = true };

                foreach (var column in budgettx.ColumnLabels)
                {
                    var budgetval = budgettx[column, labelrow];
                    var spentval = monthlth[column, labelrow];
                    var remaining = spentval - budgetval;

                    result[column, labelrow] = remaining;
                    result[column, labelrowbudget] = budgetval;
                    result[column, labelrowactual] = spentval;
                }
            }

            return result;
        }

        private Table<Label, Label, decimal> TwoLevelReport(IEnumerable<IGrouping<int, IReportable>> outergroups)
        {
            var result = new Table<Label, Label, decimal>();

            // Create a grouping of results.
            //
            // For this one we want rows: Category, cols: Month, filter: Year

            // This is not working :(
            // https://docs.microsoft.com/en-us/dotnet/csharp/linq/create-a-nested-group
            /*
            var qq = from tx in _context.Transactions
                     where tx.Timestamp.Year == Year && ! string.IsNullOrEmpty(tx.Category)
                     group tx by tx.Timestamp.Month into months
                     from categories in (from tx in months group tx by tx.Category)
                     group categories by months.Key;
            */

            var labeltotal = new Label() { Order = 10000, Value = "TOTAL" };
            var labelempty = new Label() { Order = 9999, Value = "Blank" };

            if (outergroups != null)
                foreach (var outergroup in outergroups)
                {
                    var month = outergroup.Key;
                    var labelcol = new Label() { Order = month, Value = new DateTime(Year, month, 1).ToString("MMM") };

                    if (outergroup.Count() > 0)
                    {
                        decimal outersum = 0.0M;
                        var innergroups = outergroup.GroupBy(x => x.Category);

                        foreach (var innergroup in innergroups)
                        {
                            var sum = innergroup.Sum(x => x.Amount);

                            var labelrow = labelempty;
                            if (!string.IsNullOrEmpty(innergroup.Key))
                            {
                                labelrow = new Label() { Order = 0, Value = innergroup.Key };
                            }

                            result[labelcol, labelrow] = sum;
                            outersum += sum;
                        }
                        result[labelcol, labeltotal] = outersum;
                    }
                }

            foreach (var row in result.RowLabels)
            {
                var rowsum = result.RowValues(row).Sum();
                result[labeltotal, row] = rowsum;
            }

            return result;
        }

        // 
        public static void ExportRawReportTo(ExcelWorksheet worksheet, Table<Label, Label, decimal> Model, out int rows, out int cols)
        {
            // First add the headers

            int col = 1;
            int row = 1;
            worksheet.Cells[row, col++].Value = "Category";
            worksheet.Cells[row, col++].Value = "SubCategory";
            worksheet.Cells[row, col++].Value = "Key1";
            worksheet.Cells[row, col++].Value = "Key2";
            worksheet.Cells[row, col++].Value = "Key3";
            foreach (var column in Model.ColumnLabels)
            {
                worksheet.Cells[row, col++].Value = column.Value;
            }
            ++row;

            // Add values

            foreach (var rowlabel in Model.RowLabels)
            {
                col = 1;
                worksheet.Cells[row, col++].Value = rowlabel.Value;

                if (rowlabel.Emphasis)
                    worksheet.Cells[row, col++].Value = "Total";
                else
                    worksheet.Cells[row, col++].Value = rowlabel.SubValue;

                worksheet.Cells[row, col++].Value = rowlabel.Key1 ?? string.Empty;
                worksheet.Cells[row, col++].Value = rowlabel.Key2 ?? string.Empty;
                worksheet.Cells[row, col++].Value = rowlabel.Key3 ?? string.Empty;

                foreach (var column in Model.ColumnLabels)
                {
                    var cell = Model[column,rowlabel];
                    worksheet.Cells[row, col].Value = cell;
                    worksheet.Cells[row, col].Style.Numberformat.Format = "$#,##0.00";
                    ++col;
                }
                ++row;
            }

            // AutoFitColumns
            worksheet.Cells[1, 1, row, col].AutoFitColumns();

            // Result
            rows = row - 1;
            cols = col - 1;
        }


        private bool TransactionExists(int id)
        {
            return _context.Transactions.Any(e => e.ID == id);
        }

        Task<IActionResult> IController<Models.Transaction>.Index() => Index(string.Empty, string.Empty, string.Empty, string.Empty, null);

        Task<IActionResult> IController<Models.Transaction>.Edit(int id, Models.Transaction item) => Edit(id, false, item);

        Task<IActionResult> IController<Models.Transaction>.Upload(List<IFormFile> files) => Upload(files, string.Empty);

        Task<IActionResult> IController<Models.Transaction>.Download() => Download(false, false);
    }
}
