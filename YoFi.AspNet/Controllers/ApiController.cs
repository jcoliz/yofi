using Common.AspNet;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoFi.AspNet.Data;
using YoFi.Core;
using YoFi.Core.Models;
using YoFi.Core.Reports;
using YoFi.Core.Repositories;

namespace YoFi.AspNet.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    public class ApiController : Controller
    {
        #region Fields
        private readonly ApplicationDbContext _context;
        private readonly IStorageService _storage;
        #endregion

        #region Constructor
        public ApiController(ApplicationDbContext context, IStorageService storage = null)
        {
            _context = context;
            _storage = storage;
        }
        #endregion

        #region Internals
        private async Task<Transaction> LookupTransactionAsync(int id,bool splits = false)
        {
            IQueryable<Transaction> transactions;
            transactions = _context.Transactions.Where(m => m.ID == id);
            if (splits)
                transactions = transactions.Include(x => x.Splits);

            var any = await transactions.AnyAsync();
            if (!any)
                throw new KeyNotFoundException("Item not found");

            var single = await transactions.SingleAsync();
            return single;
        }

        #endregion

        #region Ajax Handlers

        [HttpPost("ApplyPayee/{id}")]
        [Authorize(Policy = "CanWrite")]
        public async Task<ApiResult> ApplyPayee(int id)
        {
            try
            {
                var transaction = await LookupTransactionAsync(id);

                // Handle payee auto-assignment

                Payee payee = null;

                // Product Backlog Item 871: Match payee on regex, optionally
                var regexpayees = _context.Payees.Where(x => x.Name.StartsWith("/") && x.Name.EndsWith("/"));
                foreach (var regexpayee in regexpayees)
                {
                    var regex = new Regex(regexpayee.Name[1..^2]);
                    if (regex.Match(transaction.Payee).Success)
                    {
                        payee = regexpayee;
                        break;
                    }
                }

                // See if the payee exists outright
                if (payee == null)
                {
                    payee = await _context.Payees.FirstOrDefaultAsync(x => transaction.Payee.Contains(x.Name));
                }

                if (payee == null)
                    throw new KeyNotFoundException("Payee unknown");

                transaction.Category = payee.Category;
                _context.Update(transaction);
                await _context.SaveChangesAsync();

                return new ApiResult(payee);
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        [HttpPost("Hide/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<ApiResult> HideTransaction(int id, bool value)
        {
            try
            {
                var transaction = await LookupTransactionAsync(id);

                transaction.Hidden = value;
                _context.Update(transaction);
                await _context.SaveChangesAsync();

                return new ApiResult();
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<ApiResult> EditTransaction(int id, bool? duplicate, [Bind("ID,Timestamp,Amount,Memo,Payee,Category,SubCategory,BankReference,ReceiptUrl")] Transaction transaction)
        {
            try
            {
                if (id != transaction.ID && duplicate != true)
                    throw new Exception("not found");

                if (!ModelState.IsValid)
                    throw new Exception("invalid");

                if (duplicate == true)
                {
                    transaction.ID = 0;
                    _context.Add(transaction);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _context.Update(transaction);
                    await _context.SaveChangesAsync();
                }

                return new ApiResult(transaction);
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        [HttpPost("UpReceipt/{id}")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<ApiResult> UpReceipt(int id, IFormFile file)
        {
            try
            {
                var transaction = await LookupTransactionAsync(id);

                if (! string.IsNullOrEmpty(transaction.ReceiptUrl))
                    throw new ApplicationException($"This transaction already has a receipt. Delete the current receipt before uploading a new one.");

                //
                // Save the file to blob storage
                //
                // TODO: Consolodate this with the exact same copy which is in TransactionsController
                //

                if (null == _storage)
                    throw new InvalidOperationException("Unable to upload receipt. Azure Blob Storage is not configured for this application.");

                string blobname = id.ToString();

                using (var stream = file.OpenReadStream())
                {
                    // Upload the file
                    await _storage.UploadBlobAsync(blobname, stream, file.ContentType);
                }

                // Save it in the Transaction

                transaction.ReceiptUrl = blobname;
                _context.Update(transaction);
                await _context.SaveChangesAsync();

                return new ApiResult();
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        // TODO: I don't think this is actually used anywhere. Perhaps the original
        // idea was that I could drag/drop an XLS file onto a transaction, and
        // it would UpSplits that??

        [HttpPost("UpSplits/{id}")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<ApiResult> UpSplits(int id, IFormFile file)
        {
            try
            {
                var transaction = await LookupTransactionAsync(id,splits:true);

                var incoming = new HashSet<Split>();
                // Extract submitted file into a list objects

                if (file.FileName.ToLower().EndsWith(".xlsx"))
                {
                    using var stream = file.OpenReadStream();
                    using var ssr = new SpreadsheetReader();
                    ssr.Open(stream);
                    var items = ssr.Deserialize<Split>(exceptproperties: new string[] { "ID" });
                    incoming.UnionWith(items);
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

                return new ApiResult(transaction);
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        [HttpGet("cat-ac")]
        [Authorize(Policy = "CanRead")]
        public List<string> CategoryAutocomplete(string q)
        {
            const int numresults = 10;

            // Look for top N recent categories in transactions, first.
            var txd = _context.Transactions.Where(x => x.Timestamp > DateTime.Now.AddMonths(-12) && x.Category.Contains(q)).GroupBy(x => x.Category).Select(g => new { Key = g.Key, Value = g.Count() }).OrderByDescending(x => x.Value).Take(numresults);

            // There are also some categories in splits. Get the top N there too.
            var spd = _context.Splits.Include(x => x.Transaction).Where(x => x.Transaction.Timestamp > DateTime.Now.AddMonths(-12) && x.Category.Contains(q)).GroupBy(x => x.Category).Select(g => new { Key = g.Key, Value = g.Count() }).OrderByDescending(x => x.Value).Take(numresults);

            // Merge the results

            // https://stackoverflow.com/questions/2812545/how-do-i-sum-values-from-two-dictionaries-in-c
            return txd.Concat(spd).GroupBy(x => x.Key).Select(x => new { Key = x.Key, Value = x.Sum(g => g.Value) }).OrderByDescending(x => x.Value).Take(numresults).Select(x => x.Key).ToList();

            /* Just want to say how impressed I am with myself for getting this query to entirely run on server side :D
             * 
                  SELECT TOP(@__p_1) [t3].[Key]
                  FROM (
                      SELECT [t0].[Category] AS [Key], [t0].[c] AS [Value]
                      FROM (
                          SELECT TOP(@__p_1) [t].[Category], COUNT(*) AS [c]
                          FROM [Transactions] AS [t]
                          WHERE ([t].[Timestamp] > DATEADD(month, CAST(-12 AS int), GETDATE())) AND ((@__q_0 = N'') OR (CHARINDEX(@__q_0, [t].[Category]) > 0))
                          GROUP BY [t].[Category]
                          ORDER BY COUNT(*) DESC
                      ) AS [t0]
                      UNION ALL
                      SELECT [t2].[Category] AS [Key], [t2].[c] AS [Value]
                      FROM (
                          SELECT TOP(@__p_1) [s].[Category], COUNT(*) AS [c]
                          FROM [Split] AS [s]
                          INNER JOIN [Transactions] AS [t1] ON [s].[TransactionID] = [t1].[ID]
                          WHERE ([t1].[Timestamp] > DATEADD(month, CAST(-12 AS int), GETDATE())) AND ((@__q_0 = N'') OR (CHARINDEX(@__q_0, [s].[Category]) > 0))
                          GROUP BY [s].[Category]
                          ORDER BY COUNT(*) DESC
                      ) AS [t2]
                  ) AS [t3]
                  GROUP BY [t3].[Key]
                  ORDER BY SUM([t3].[Value]) DESC
            */
        }

        #endregion

        #region External API

        [HttpGet]
        [ApiBasicAuthorization]
        public ApiResult Get()
        {
            return new ApiResult();
        }

        [HttpGet("{id}", Name = "Get")]
        [ApiBasicAuthorization]
        public async Task<ApiResult> Get(int id)
        {
            try
            {
                return new ApiResult(await LookupTransactionAsync(id));
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        [HttpGet("ReportV2/{id}")]
        [ApiBasicAuthorization]
        public ActionResult ReportV2([Bind("id,year,month,showmonths,level")] ReportParameters parms, [FromServices] IReportEngine reports)
        {
            try
            {
                var result = reports.Build(parms);
                var json = result.ToJson();

                return Content(json,"application/json");
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, statusCode: 500);
            }
        }

        [HttpGet("txi")]
        [ApiBasicAuthorization]
        public async Task<ActionResult> GetTransactions(string q = null)
        {
            try
            {
                var qbuilder = new TransactionsQueryBuilder(_context.Transactions);
                qbuilder.Build(q);

                return new JsonResult(await qbuilder.Query.ToListAsync());
            }
            catch (Exception ex)
            {
                return Problem(detail:ex.Message,statusCode:500);
            }
        }

        /// <summary>
        /// Returns the highest ID for the <paramref name="kind"/> of item supplied
        /// </summary>
        /// <param name="kind">Kind of item to get max is</param>
        /// <returns></returns>
        [HttpGet("maxid/{kind}")]
        [ApiBasicAuthorization]
        public async Task<ActionResult> MaxId(string kind)
        {
            try
            {
                if (kind == null)
                    throw new ArgumentNullException(nameof(kind));

                if (kind == "payees")
                {
                    var maxitem = await _context.Payees.OrderByDescending(x => x.ID).Take(1).FirstOrDefaultAsync();
                    var id = maxitem?.ID ?? 0;
                    return new JsonResult(new ApiResult(id));
                }
                else
                    throw new ArgumentException($"Unknown kind {kind}", nameof(kind));

            }
            catch (Exception ex)
            {
                return new JsonResult(new ApiResult(ex));
            }
        }

        /// <summary>
        /// Remove all test data from the system
        /// </summary>
        /// <remarks>
        /// Deletes all objects of all types with __TEST__ in their category.
        /// Used by funtional tests to clean themselves up
        /// </remarks>
        /// <returns></returns>
        [HttpPost("ClearTestData/{id}")]
        [ApiBasicAuthorization]
        public async Task<ActionResult> ClearTestData(string id)
        {
            const string testmarker = "__test__";

            try
            {
                if (id.Contains("payee"))
                    _context.Payees.RemoveRange(_context.Payees.Where(x => x.Category.Contains(testmarker)));

                if (id.Contains("budgettx"))
                    _context.BudgetTxs.RemoveRange(_context.BudgetTxs.Where(x => x.Category.Contains(testmarker)));

                if (id.Contains("trx"))
                {
                    _context.Transactions.RemoveRange(_context.Transactions.Where(x => x.Category.Contains(testmarker) || x.Memo.Contains(testmarker)));
                    _context.Splits.RemoveRange(_context.Splits.Where(x => x.Category.Contains(testmarker)));
                }

                await _context.SaveChangesAsync();

                return new JsonResult(new ApiResult());
            }
            catch (Exception ex)
            {
                return new JsonResult(new ApiResult(ex));
            }
        }

        #endregion
    }

    /// <summary>
    /// The standard result type we return from these APIs
    /// </summary>
    public class ApiResult
    {
        /// <summary>
        /// Whether the request was successful
        /// </summary>
        public bool Ok { get; set; } = true;

        /// <summary>
        /// Item returned in the request
        /// </summary>
        public object Item { get; private set; } = null;

        /// <summary>
        /// Error encountered, only set if OK == false
        /// </summary>
        public string Error { get; private set; } = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ApiResult() { }

        /// <summary>
        /// Typical constructor
        /// </summary>
        /// <param name="o">Object result</param>
        public ApiResult(object o)
        {
            if (o is Exception)
            {
                var ex = o as Exception;
                Error = $"{ex.GetType().Name}: {ex.Message}";
                Ok = false;
            }
            else
                Item = o;
        }
    }
}
