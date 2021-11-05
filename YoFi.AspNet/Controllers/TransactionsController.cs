using Ardalis.Filters;
using Common.AspNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Boilerplate.Models;
using YoFi.AspNet.Data;
using YoFi.Core.Importers;
using YoFi.Core.Repositories;
using Transaction = YoFi.Core.Models.Transaction;

namespace YoFi.AspNet.Controllers
{
    [Authorize(Policy = "CanRead")]
    public class TransactionsController : Controller, IController<Transaction>
    {
        #region Public Properties
        public static int PageSize { get; } = 25;

        #endregion

        #region Constructor

        public TransactionsController(ITransactionRepository repository, ApplicationDbContext context)
        {
            _context = context;
            _repository = repository;
        }

        #endregion

        #region Action Handlers: Index & Helpers

        public class IndexViewModel: IViewParameters
        {
            #region Public Properties -- Used by View to display
            public IEnumerable<TransactionIndexDto> Items { get; set; }
            public PageDivider Divider { get; set; }
            public string QueryParameter { get; set; }
            public string ViewParameter
            {
                get
                {
                    return _View;
                }
                set
                {
                    _View = value;
                    ShowHidden = ViewParameter?.ToLowerInvariant().Contains("h") == true;
                    ShowSelected = ViewParameter?.ToLowerInvariant().Contains("s") == true;
                }
            }
            private string _View;

            public string OrderParameter
            {
                get
                {
                    return (_Order == default_order) ? null : _Order;
                }
                set
                {
                    _Order = string.IsNullOrEmpty(value) ? default_order : value;
                }
            }
            private string _Order;
            const string default_order = "dd";

            public int? PageParameter
            {
                get
                {
                    return (_PageParameter == default_page) ? null : (int?)_PageParameter;
                }
                set
                {
                    _PageParameter = value ?? default_page;
                }
            }
            private int _PageParameter = default_page;
            const int default_page = 1;

            public bool ShowHidden { get; set; }
            public bool ShowSelected { get; set; }

            public string DateSortParm => (_Order == "dd") ? "da" : null; /* not "dd", which is default */
            public string PayeeSortParm => (_Order == "pa") ? "pd" : "pa";
            public string CategorySortParm => (_Order == "ca") ? "cd" : "ca";
            public string AmountSortParm => (_Order == "aa") ? "as" : "aa";
            public string BankReferenceSortParm => (_Order == "ra") ? "rd" : "ra";
            public string ToggleHidden => (ShowHidden ? string.Empty : "h") + (ShowSelected ? "s" : string.Empty);
            public string ToggleSelected => (ShowHidden ? "h" : string.Empty) + (ShowSelected ? string.Empty : "s");
            #endregion

            internal IQueryable<Transaction> Query { get; set; }

            /// <summary>
            /// Interprets the "o" (Order) parameter on a transactions search
            /// </summary>
            /// <remarks>
            /// Public so can be used by other controllers.
            /// </remarks>
            /// <param name="result">Initial query to further refine</param>
            /// <param name="p">Order parameter</param>
            /// <returns>Resulting query refined by <paramref name="o"/></returns>
            internal void ApplyOrderParameter()
            {
                Query = _Order switch
                {
                    // Coverlet finds cyclomatic complexity of 42 in this function!!?? No clue why it's not just 10.
                    "aa" => Query.OrderBy(s => s.Amount),
                    "ad" => Query.OrderByDescending(s => s.Amount),
                    "ra" => Query.OrderBy(s => s.BankReference),
                    "rd" => Query.OrderByDescending(s => s.BankReference),
                    "pa" => Query.OrderBy(s => s.Payee),
                    "pd" => Query.OrderByDescending(s => s.Payee),
                    "ca" => Query.OrderBy(s => s.Category),
                    "cd" => Query.OrderByDescending(s => s.Category),
                    "da" => Query.OrderBy(s => s.Timestamp).ThenBy(s => s.Payee),
                    "dd" => Query.OrderByDescending(s => s.Timestamp).ThenBy(s => s.Payee),
                    _ => Query
                };
            }

            internal async Task ApplyPageParameterAsync()
            {
                Query = await Divider.ItemsForPage(Query, _PageParameter);
                Divider.ViewParameters = this;
            }

            internal async Task ExecuteQueryAsync()
            {
                if (ShowHidden || ShowSelected)
                {
                    // Get the long form
                    Items = await Query.Select(t => new TransactionIndexDto()
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
                    Items = await Query.Select(t => new TransactionIndexDto()
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
                public static explicit operator Transaction(TransactionIndexDto o) => new Transaction()
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

            internal void ApplyViewParameter()
            {
                if (!ShowHidden)
                    Query = Query.Where(x => x.Hidden != true);
            }
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
            try
            {
                var viewmodel = new IndexViewModel()
                {
                    Divider = new PageDivider() { PageSize = PageSize },
                    Query = _repository.ForQuery(q)
                };

                //
                // Process QUERY (Q) parameter
                //

                viewmodel.QueryParameter = q;

                //
                // Process VIEW (V) parameter
                //

                viewmodel.ViewParameter = v;
                viewmodel.ApplyViewParameter();

                //
                // Process ORDER (O) parameter
                //

                viewmodel.OrderParameter = o;
                viewmodel.ApplyOrderParameter();

                //
                // Process PAGE (P) parameter
                //

                viewmodel.PageParameter = p;
                await viewmodel.ApplyPageParameterAsync();

                //
                // Execute Query
                //

                await viewmodel.ExecuteQueryAsync();

                return View(viewmodel);
            }
            catch (ArgumentException)
            {
                return BadRequest();
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

        #region Action Handlers: Create (Done)

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
            var result = await _repository.AddSplitToAsync(id);

            return RedirectToAction("Edit", "Splits", new { id = result });
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

        #region Action Handlers: Edit (Done)

        [ValidateTransactionExists]
        public async Task<IActionResult> Edit(int? id, [FromServices] IPayeeRepository payeeRepository)
        {
            var transaction = await _repository.GetWithSplitsByIdAsync(id);
            if (string.IsNullOrEmpty(transaction.Category))
            {
                var category = await payeeRepository.GetCategoryMatchingPayeeAsync(transaction.StrippedPayee);
                if (category != null)
                {
                    transaction.Category = category;
                    ViewData["AutoCategory"] = true;
                }
            }

            return View(transaction);
        }

        [ValidateTransactionExists]
        public async Task<IActionResult> EditModal(int? id, [FromServices] IPayeeRepository payeeRepository)
        {
            var transaction = await _repository.GetWithSplitsByIdAsync(id);
            if (string.IsNullOrEmpty(transaction.Category))
            {
                var category = await payeeRepository.GetCategoryMatchingPayeeAsync(transaction.StrippedPayee);
                if (category != null)
                {
                    transaction.Category = category;
                    ViewData["AutoCategory"] = true;
                }
            }

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
            if (id != transaction.ID && duplicate != true)
                return BadRequest();

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

        [HttpPost]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> BulkEdit(string Category)
        {
            await _repository.BulkEdit(Category);

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Action Handlers: Delete (Done)

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

        #region Action Handlers: Download (Done)

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

        #region Action Handlers: Others (Error) (Done)

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        #endregion

        #region Action Handlers: Receipts (done)

        [HttpPost]
        [Authorize(Policy = "CanWrite")]
        [ValidateTransactionExists]
        [ValidateFilesProvided(multiplefilesok:false)]
        [ValidateStorageAvailable]
        public async Task<IActionResult> UpReceipt(List<IFormFile> files, int id)
        {
            var transaction = await _repository.GetByIdAsync(id);

            var formFile = files.Single();
            using var stream = formFile.OpenReadStream();
            await _repository.UploadReceiptAsync(transaction,stream, formFile.ContentType);

            return RedirectToAction(nameof(Edit), new { id });
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
            var (stream, contenttype, name) = await _repository.GetReceiptAsync(transaction);
            return File(stream, contenttype, name);
        }

        #endregion 

        #region Action Handlers: Import Pipeline (done)
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
        public async Task<IActionResult> Upload(List<IFormFile> files, [FromServices] TransactionImporter importer)
        {
            // Open each file in turn, and send them to the importer

            foreach (var formFile in files)
            {
                using var stream = formFile.OpenReadStream();

                var filetype = Path.GetExtension(formFile.FileName).ToLowerInvariant();

                if (filetype == ".ofx")
                    await importer.QueueImportFromOfxAsync(stream);
                else if (filetype == ".xlsx")
                    importer.QueueImportFromXlsx(stream);
            }

            // Process the imported files
            await importer.ProcessImportAsync();

            // This is kind of a crappy way to communicate the potential false negative conflicts.
            // If user returns to Import page directly, these highlights will be lost. Really probably
            // should persist this to the database somehow. Or at least stick it in the session??
            return RedirectToAction(nameof(Import), new { highlight = string.Join(':', importer.HighlightIDs) });
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

                if (command == "cancel")
                {
                    await _repository.CancelImportAsync();
                    return RedirectToAction(nameof(Import));
                }
                else if (command == "ok")
                {
                    await _repository.FinalizeImportAsync();
                    return RedirectToAction(nameof(Index));
                }
                else
                    throw new ArgumentException();
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        [Authorize(Policy = "CanWrite")]
        [ValidateFilesProvided(multiplefilesok: true)]
        [ValidateTransactionExists]
        public async Task<IActionResult> UpSplits(List<IFormFile> files, int id, [FromServices] SplitImporter importer)
        {
            var transaction = await _repository.GetWithSplitsByIdAsync(id);

            importer.Target = transaction;

            foreach (var file in files.Where(x => Path.GetExtension(x.FileName).ToLowerInvariant() == ".xlsx"))
            {
                using var stream = file.OpenReadStream();
                importer.QueueImportFromXlsx(stream);
            }

            await importer.ProcessImportAsync();

            return RedirectToAction("Edit", new { id = id });
        }
        #endregion

        #region Internals

        private readonly ApplicationDbContext _context;

        private readonly ITransactionRepository _repository;

        /// <summary>
        /// Current default year
        /// </summary>
        /// <remarks>
        /// If you set this in the reports, it applies throughout the app,
        /// defaulting to that year.
        /// 
        /// Note this is public so that tests could set it if needed. Currently, unit tests rely on
        /// DateTime.Now for test data, so this is probably OK.
        /// </remarks>
        public int Year
        {
            get
            {
                if (!_Year.HasValue)
                {
                    var value = this.HttpContext?.Session.GetString(nameof(Year));
                    if (string.IsNullOrEmpty(value))
                    {
                        Year = DateTime.Now.Year;
                    }
                    else
                    {
                        _Year = (int.TryParse(value, out int y)) ? y : DateTime.Now.Year;
                    }
                }

                return _Year.Value;
            }
            set
            {
                _Year = value;

                var serialisedDate = _Year.ToString();
                this.HttpContext?.Session.SetString(nameof(Year), serialisedDate);
            }
        }
        private int? _Year = null;

        #endregion

        #region IController
        Task<IActionResult> IController<Transaction>.Index() => Index();

        Task<IActionResult> IController<Transaction>.Edit(int id, Transaction item) => Edit(id, false, item);

        Task<IActionResult> IController<Transaction>.Download() => Download(false);

        Task<IActionResult> IController<Transaction>.Upload(List<IFormFile> files) => throw new NotImplementedException();

        Task<IActionResult> IController<Transaction>.Edit(int? id)
        {
            throw new NotImplementedException();
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
