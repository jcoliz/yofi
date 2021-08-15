using Common.NET;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoFi.AspNet.Common;
using YoFi.AspNet.Controllers.Reports;
using YoFi.AspNet.Data;
using YoFi.AspNet.Models;

namespace YoFi.AspNet.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    public class ApiController : Controller
    {
        private readonly ApplicationDbContext _context;
        private IPlatformAzureStorage _storage;
        private readonly IConfiguration _configuration;

        public ApiController(ApplicationDbContext context, IConfiguration configuration, IPlatformAzureStorage storage = null)
        {
            _context = context;
            _storage = storage;
            _configuration = configuration;
        }

        [HttpGet]
        public ApiResult Get()
        {
            return new ApiResult();
        }

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

        [HttpGet("{id}", Name = "Get")]
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

        [HttpPost("ApplyPayee/{id}")]
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
                    var regex = new Regex(regexpayee.Name.Substring(1, regexpayee.Name.Length - 2));
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
                transaction.SubCategory = payee.SubCategory;
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
        public async Task<ApiResult> Hide(int id, bool value)
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

        [HttpPost("Select/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<ApiResult> Select(int id, bool value)
        {
            try
            {
                var transaction = await LookupTransactionAsync(id);

                transaction.Selected = value;
                _context.Update(transaction);
                await _context.SaveChangesAsync();

                return new ApiResult();
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        [HttpPost("SelectPayee/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<ApiResult> SelectPayee(int id, bool value)
        {
            try
            {
                var payee = await _context.Payees
                    .SingleAsync(m => m.ID == id);

                payee.Selected = value;
                _context.Update(payee);
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
        public async Task<ApiResult> Edit(int id, bool? duplicate, [Bind("ID,Timestamp,Amount,Memo,Payee,Category,SubCategory,BankReference,ReceiptUrl")] Models.Transaction transaction)
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

                if (null == _storage)
                    throw new InvalidOperationException("Unable to upload receipt. Azure Blob Storage is not configured for this application.");

                _storage.Initialize();

                string contenttype = null;

                using (var stream = file.OpenReadStream())
                {

                    // Upload the file
                    await _storage.UploadToBlob(BlobStoreName, id.ToString(), stream, file.ContentType);

                    // Remember the content type
                    // TODO: This can just be a true/false bool, cuz now we store content type in blob store.
                    contenttype = file.ContentType;
                }

                // Save it in the Transaction

                if (null != contenttype)
                {
                    transaction.ReceiptUrl = contenttype;
                    _context.Update(transaction);
                    await _context.SaveChangesAsync();
                }

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
        public async Task<ApiResult> UpSplits(int id, IFormFile file)
        {
            try
            {
                var transaction = await LookupTransactionAsync(id,splits:true);

                var incoming = new HashSet<Models.Split>();
                // Extract submitted file into a list objects

                if (file.FileName.ToLower().EndsWith(".xlsx"))
                {
                    using (var stream = file.OpenReadStream())
                    using (var ssr = new SpreadsheetReader())
                    {
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

                return new ApiResult(transaction);
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        [HttpPost("EditPayee/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<ApiResult> EditPayee(int id, bool? duplicate, [Bind("ID,Name,Category,SubCategory")] Models.Payee payee)
        {
            try
            {
                if (id != payee.ID && duplicate != true)
                    throw new Exception("not found");

                if (!ModelState.IsValid)
                    throw new Exception("invalid");

                if (duplicate == true)
                {
                    payee.ID = 0;
                    _context.Add(payee);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _context.Update(payee);
                    await _context.SaveChangesAsync();
                }

                return new ApiResult(payee);
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }


        [HttpPost("AddPayee")]
        [ValidateAntiForgeryToken]
        public async Task<ApiResult> AddPayee([Bind("Name,Category,SubCategory")] Payee payee)
        {
            try
            {
                if (!ModelState.IsValid)
                    throw new Exception("invalid");

                _context.Add(payee);
                await _context.SaveChangesAsync();
                return new ApiResult(payee);
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        private void CheckApiAuth(IHeaderDictionary Headers)
        {
            if (!Headers.ContainsKey("Authorization"))
                throw new UnauthorizedAccessException();

            var authorization = Headers["Authorization"].Single();
            if (!authorization.StartsWith("Basic "))
                throw new UnauthorizedAccessException();

            var base64 = authorization.Substring(6);
            var credentialBytes = Convert.FromBase64String(base64);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
            var username = credentials[0];
            var password = credentials[1];

            var expectedpassword = _configuration["Api:Key"];

            if (string.IsNullOrEmpty(expectedpassword))
                throw new ApplicationException("Application not configured for API password access.");

            if (expectedpassword != password)
                throw new ApplicationException("Invalid password");
        }

        [HttpGet("ReportV2/{id}")]
        public ActionResult ReportV2([Bind("id,year,month,showmonths,level")] ReportBuilder.Parameters parms)
        {
            try
            {
                CheckApiAuth(Request.Headers);

                var result = new ReportBuilder(_context).BuildReport(parms);
                var json = result.ToJson();

                return Content(json,"application/json");
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex);
            }
        }

        [HttpGet("cat-ac")]
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
    }

    public class ApiResult
    {
        public bool Ok { get; set; } = true;

        public object Item { get; private set; } = null;

        public string Error { get; private set; } = null;

        public ApiResult() { }

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
