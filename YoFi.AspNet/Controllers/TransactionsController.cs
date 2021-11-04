using Common.AspNet;
using Common.NET;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Boilerplate.Models;
using YoFi.AspNet.Data;
using YoFi.Core.Models;
using YoFi.Core.Importers;
using YoFi.Core.Quieriers;
using YoFi.Core.Reports;
using Transaction = YoFi.Core.Models.Transaction;
using Ardalis.Filters;
using YoFi.Core.Repositories;

namespace YoFi.AspNet.Controllers
{
    [Authorize(Policy = "CanRead")]
    public class TransactionsController : Controller, IController<Transaction>
    {
        #region Public Properties
        public static int PageSize { get; } = 25;

        #endregion

        #region Constructor

        public TransactionsController(ITransactionRepository repository, ApplicationDbContext context, IConfiguration config, IPlatformAzureStorage storage = null)
        {
            _context = context;
            _repository = repository;
            _storage = storage;
            _config = config;
        }

        #endregion

        #region Action Handlers: Index & Helpers

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
            try
            {
                //
                // Process QUERY (Q) parameters
                //

                ViewData["Query"] = q;

                var qbuilder = new TransactionsQueryBuilder(_context.Transactions.Include(x => x.Splits));
                qbuilder.Build(q);
                var result = qbuilder.Query;

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
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
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

        #endregion

        #region Action Handlers: Get Details (Done)

        /// <summary>
        /// Retrieve a single transaction
        /// </summary>
        /// <param name="id">ID of the desired transaction</param>
        /// <returns></returns>
        [ValidateTransactionExists]
        public async Task<IActionResult> Details(int? id)
        {
            return View(await _repository.GetByIdAsync(id));
        }

        #endregion

        #region Action Handlers: Create

        /// <summary>
        /// Create a new split for the specified transaction <paramref name="id"/>
        /// </summary>
        /// <param name="id">ID of the transaction</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateTransactionExists]
        public async Task<IActionResult> CreateSplit(int id)
        {
            var transaction = await _repository.GetWithSplitsByIdAsync(id);
            var result = _repository.AddSplitTo(transaction);
            await _repository.UpdateAsync(transaction);

            return RedirectToAction("Edit", "Splits", new { id = result.ID });
        }

        /// <summary>
        /// View for create transaction page, which is empty because we're creating a transaction from empty
        /// </summary>
        /// <returns></returns>
        public Task<IActionResult> Create()
        {
            return Task.FromResult(View() as IActionResult);
        }

        /// <summary>
        /// Actually create the given <paramref name="transaction"/>
        /// </summary>
        /// <param name="transaction">FUlly-formed transaction to create</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateModel]
        public async Task<IActionResult> Create([Bind("ID,Timestamp,Amount,Memo,Payee,Category,SubCategory,BankReference")] Transaction transaction)
        {
            await _repository.AddAsync(transaction);
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Action Handlers: Edit

        [ValidateTransactionExists]
        public async Task<IActionResult> Edit(int? id)
        {
            var transaction = await _repository.GetWithSplitsByIdAsync(id);
            var didassign = await _repository.AssignPayeeAsync(transaction);
            ViewData["AutoCategory"] = didassign;

            return View(transaction);
        }

        [ValidateTransactionExists]
        public async Task<IActionResult> EditModal(int? id)
        {
            var transaction = await _repository.GetWithSplitsByIdAsync(id);
            var didassign = await _repository.AssignPayeeAsync(transaction);
            ViewData["AutoCategory"] = didassign;

            return PartialView("EditPartial", transaction);

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
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateModel]
        [ValidateTransactionExists]
        public async Task<IActionResult> Edit(int id, bool? duplicate, [Bind("ID,Timestamp,Amount,Memo,Payee,Category,SubCategory,BankReference")] Transaction transaction)
        {
            try
            {
                if (id != transaction.ID && duplicate != true)
                    throw new ArgumentException();

                if (duplicate == true)
                {
                    transaction.ID = 0;
                    await _repository.AddAsync(transaction);
                }
                else
                {
                    // Bug #846: This Edit function is not allowed to alter the
                    // ReceiptUrl. So we must preserve whatever was there.

                    // TODO: FirstOrDefaultAsync()
                    var oldreceipturl = _repository.All.Where(x => x.ID == id).Select(x => x.ReceiptUrl).FirstOrDefault();
                    
                    transaction.ReceiptUrl = oldreceipturl;

                    await _repository.UpdateAsync(transaction);
                }
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> BulkEdit(string Category)
        {
            await _repository.BulkEdit(Category);

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Action Handlers: Delete

        // GET: Transactions/Delete/5
        [ValidateTransactionExists]
        public async Task<IActionResult> Delete(int? id) => await Details(id);

        // POST: Transactions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateTransactionExists]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _repository.RemoveAsync(await _repository.GetByIdAsync(id));

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Action Handlers: Download

        [HttpPost]
        public async Task<IActionResult> Download(bool allyears, string q = null)
        {
            Stream stream = await _repository.AsSpreadsheet(Year,allyears,q);

            IActionResult result = File(stream, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName: "Transactions.xlsx");

            return result;
        }

        public IActionResult DownloadPartial()
        {
            return PartialView();
        }

        #endregion

        #region Action Handlers: Others (Report, Error)

        // GET: Transactions/Report
        [ValidateModel]
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
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        #endregion

        #region Action Handlers: Receipts

        [HttpPost]
        [Authorize(Policy = "CanWrite")]
        [ValidateTransactionExists]
        [ValidateFilesProvided(multiplefilesok:false)]
        [ValidateStorageAvailable]
        public async Task<IActionResult> UpReceipt(List<IFormFile> files, int id)
        {
            if (null == _storage)
                throw new InvalidOperationException("Unable to upload receipt. Azure Blob Storage is not configured for this application.");

            var transaction = await _repository.GetByIdAsync(id);

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
            await _repository.UpdateAsync(transaction);

            return Redirect($"/Transactions/Edit/{id}");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateStorageAvailable]
        [ValidateTransactionExists]
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
            var transaction = await _repository.GetByIdAsync(id);
            transaction.ReceiptUrl = null;
            await _repository.UpdateAsync(transaction);

            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpGet]
        [ValidateTransactionExists]
        [ValidateStorageAvailable]
        public async Task<IActionResult> GetReceipt(int id)
        {

            var transaction = await _repository.GetByIdAsync(id);

            if (string.IsNullOrEmpty(transaction.ReceiptUrl))
                return new NotFoundObjectResult("Transaction has no receipt");

            var blobname = id.ToString();

            // See Bug #991: Production bug: Receipts before 5/20/2021 don't download
            // If the ReceiptUrl contains an int value, use THAT for the blobname instead.

            if (Int32.TryParse(transaction.ReceiptUrl,out _))
                blobname = transaction.ReceiptUrl;

            _storage.Initialize();
            var stream = new MemoryStream();
            var contenttype = await _storage.DownloadBlob(BlobStoreName, blobname, stream);

            // Work around previous versions which did NOT store content type in blob store.
            if ("application/octet-stream" == contenttype)
                contenttype = "application/pdf";

            stream.Seek(0, SeekOrigin.Begin);
            return File(stream, contenttype, id.ToString());
        }

        #endregion

        #region Action Handlers: Import Pipeline
        /// <summary>
        /// Upload a file of transactions to be imported
        /// </summary>
        /// <remarks>
        /// This is the first step of the upload/import pipeline. The user is first
        /// giving us the transactions here.
        /// </remarks>
        /// <param name="files">Files to import</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Policy = "CanWrite")]
        [ValidateFilesProvided(multiplefilesok: true)]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            // Open each file in turn, and send them to the importer

            var importer = new TransactionImporter(_context);

            foreach (var formFile in files)
            {
                var filetype = Path.GetExtension(formFile.FileName).ToLowerInvariant() switch
                {
                    ".ofx" => TransactionImporter.ImportableFileTypeEnum.Ofx,
                    ".xlsx" => TransactionImporter.ImportableFileTypeEnum.Xlsx,
                    _ => TransactionImporter.ImportableFileTypeEnum.Invalid
                };

                if (filetype != TransactionImporter.ImportableFileTypeEnum.Invalid)
                {
                    using var stream = formFile.OpenReadStream();
                    await importer.LoadFromAsync(stream, filetype);
                }
            }

            // Process the imported files
            await importer.Process();

            // This is kind of a crappy way to communicate the potential false negative conflicts.
            // If user returns to Import page directly, these highlights will be lost. Really probably
            // should persist this to the database somehow. Or at least stick it in the session??
            return RedirectToAction(nameof(Import), new { highlight = string.Join(':', importer.HighlightIDs) });
        }

        /// <summary>
        /// Finally execute the import for all selected transactions
        /// </summary>
        /// <remarks>
        /// This is the last step in the upload/import pipeline
        /// </remarks>
        /// <param name="command">'ok' to do the import, 'cancel' to cancel it</param>
        /// <returns></returns>
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

        /// <summary>
        /// Display the transactions which have been imported
        /// </summary>
        /// <remarks>
        /// This is the second step in the upload/import pipeline. At the point the
        /// imported transactions are in the database, but hidden and marked as 'imported'.
        /// Now we want to display those to the users to see if it's satisfactory
        /// </remarks>
        /// <param name="highlight">Colon-separated list of IDs that should get highlighted</param>
        /// <param name="p">Page number</param>
        /// <returns></returns>
        public async Task<IActionResult> Import(string highlight = null, int? p = null)
        {
            // TODO: Should add a DTO here
            IQueryable<Transaction> result = _repository.OrderedQuery.Where(x => x.Imported == true);

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

            // TODO: ToListAsync()
            // TODO: AsNoTracking()
            return View(result.ToList());
        }

        [HttpPost]
        [Authorize(Policy = "CanWrite")]
        [ValidateFilesProvided(multiplefilesok: true)]
        [ValidateTransactionExists]
        public async Task<IActionResult> UpSplits(List<IFormFile> files, int id)
        {
            var transaction = await _repository.GetWithSplitsByIdAsync(id);

            var incoming = new HashSet<Split>();
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
        #endregion

        #region Internals

        private readonly ApplicationDbContext _context;

        private readonly ITransactionRepository _repository;

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

        private async Task<Transaction> Get(int? id) => await _context.Transactions.SingleAsync(x => x.ID == id.Value);
        private async Task<Transaction> GetWithSplits(int? id) => await _context.Transactions.Include(x => x.Splits).SingleAsync(x => x.ID == id.Value);
        #endregion

        #region IController
        Task<IActionResult> IController<Transaction>.Index() => Index();

        Task<IActionResult> IController<Transaction>.Edit(int id, Transaction item) => Edit(id, false, item);

        Task<IActionResult> IController<Transaction>.Download() => Download(false);
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
