using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ManiaLabs.NET;
using ManiaLabs.Portable.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;

namespace OfxWeb.Asp.Controllers
{
    [Produces("application/json")]
    [Route("api/tx")]
    public class ApiController : Controller
    {
        private readonly ApplicationDbContext _context;

        private IPlatformAzureStorage _storage;

        public ApiController(ApplicationDbContext context, IPlatformAzureStorage storage)
        {
            _context = context;
            _storage = storage;
        }

        // GET: api/tx
        [HttpGet]
        public string Get()
        {
            return new ApiResult();
        }

        // GET: api/tx/5
        [HttpGet("{id}", Name = "Get")]
        public async Task<string> Get(int id)
        {
            try
            {
                var transaction = await _context.Transactions
                    .SingleAsync(m => m.ID == id);

                return new ApiTransactionResult(transaction);
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        // GET: api/tx/ApplyPayee/5
        [HttpGet("ApplyPayee/{id}")]
        public async Task<string> ApplyPayee(int id)
        {
            try
            {
                var transaction = await _context.Transactions.SingleAsync(m => m.ID == id);

                // Handle payee auto-assignment

                // See if the payee exists
                var payee = await _context.Payees.FirstOrDefaultAsync(x => transaction.Payee.Contains(x.Name));

                if (payee == null)
                {
                    // Product Backlog Item 871: Match payee on regex, optionally
                    var regexpayees = _context.Payees.Where(x => x.Name.StartsWith("/") && x.Name.EndsWith("/"));
                    foreach (var regexpayee in regexpayees)
                    {
                        var regex = new Regex(regexpayee.Name.Substring(1,regexpayee.Name.Length-2));
                        if (regex.Match(transaction.Payee).Success)
                        {
                            payee = regexpayee;
                            break;
                        }
                    }
                }

                if (payee == null)
                    throw new KeyNotFoundException("Payee unknown");

                transaction.Category = payee.Category;
                transaction.SubCategory = payee.SubCategory;
                _context.Update(transaction);
                await _context.SaveChangesAsync();

                return new ApiPayeeResult(payee);
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        // GET: api/tx/Hide/5
        [HttpGet("Hide/{id}")]
        public async Task<string> Hide(int id)
        {
            try
            {
                var transaction = await _context.Transactions
                    .SingleAsync(m => m.ID == id);

                transaction.Hidden = true;
                _context.Update(transaction);
                await _context.SaveChangesAsync();

                return new ApiResult();
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        // GET: api/tx/Show/5
        [HttpGet("Show/{id}")]
        public async Task<string> Show(int id)
        {
            try
            {
                var transaction = await _context.Transactions
                    .SingleAsync(m => m.ID == id);

                transaction.Hidden = false;
                _context.Update(transaction);
                await _context.SaveChangesAsync();

                return new ApiResult();
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        // GET: api/tx/Select/5
        [HttpGet("Select/{id}")]
        public async Task<string> Select(int id)
        {
            try
            {
                var transaction = await _context.Transactions
                    .SingleAsync(m => m.ID == id);

                transaction.Selected = true;
                _context.Update(transaction);
                await _context.SaveChangesAsync();

                return new ApiResult();
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        // GET: api/tx/Deselect/5
        [HttpGet("Deselect/{id}")]
        public async Task<string> Deselect(int id)
        {
            try
            {
                var transaction = await _context.Transactions
                    .SingleAsync(m => m.ID == id);

                transaction.Selected = false;
                _context.Update(transaction);
                await _context.SaveChangesAsync();

                return new ApiResult();
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        // GET: api/tx/SelectPayee/5
        [HttpGet("SelectPayee/{id}")]
        public async Task<string> SelectPayee(int id)
        {
            try
            {
                var payee = await _context.Payees
                    .SingleAsync(m => m.ID == id);

                payee.Selected = true;
                _context.Update(payee);
                await _context.SaveChangesAsync();

                return new ApiResult();
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        // GET: api/tx/DeselectPayee/5
        [HttpGet("DeselectPayee/{id}")]
        public async Task<string> DeselectPayee(int id)
        {
            try
            {
                var payee = await _context.Payees
                    .SingleAsync(m => m.ID == id);

                payee.Selected = false;
                _context.Update(payee);
                await _context.SaveChangesAsync();

                return new ApiResult();
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        // POST: api/tx/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost("Edit/{id}")]
        public async Task<string> Edit(int id, bool? duplicate, [Bind("ID,Timestamp,Amount,Memo,Payee,Category,SubCategory,BankReference,ReceiptUrl")] Models.Transaction transaction)
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

                return new ApiTransactionResult(transaction);
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        // POST: api/tx/UpReceipt/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost("UpReceipt/{id}")]
        public async Task<string> UpReceipt(int id, IFormFile file)
        {
            try
            {
                var transaction = await _context.Transactions.SingleOrDefaultAsync(m => m.ID == id);

                if (null == transaction)
                    throw new ApplicationException($"Unable to find transaction #{id}");

                if (! string.IsNullOrEmpty(transaction.ReceiptUrl))
                    throw new ApplicationException($"This transaction already has a receipt. Delete the current receipt before uploading a new one.");

                //
                // Save the file to blob storage
                //

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


        // POST: api/tx/EditPayee/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost("EditPayee/{id}")]
        public async Task<string> EditPayee(int id, bool? duplicate, [Bind("ID,Name,Category,SubCategory")] Models.Payee payee)
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

                return new ApiPayeeResult(payee);
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }


        // POST: api/tx/AddPayee
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        // TODO: Move to a payee api controller and rename to POST api/payee/Create
        [HttpPost("AddPayee")]
        public async Task<string> AddPayee([Bind("Name,Category,SubCategory")] Payee payee)
        {
            try
            {
                if (!ModelState.IsValid)
                    throw new Exception("invalid");

                _context.Add(payee);
                await _context.SaveChangesAsync();
                return new ApiPayeeResult(payee);
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        // GET: api/tx/report/2020
        [HttpGet("Report/{topic}")]
        public async Task<ActionResult> Report(string topic, int year, string key, string constraint)
        {
            try
            {
                if (!Request.Headers.ContainsKey("Authorization"))
                    throw new UnauthorizedAccessException();

                var authorization = Request.Headers["Authorization"].Single();
                if (!authorization.StartsWith("Basic "))
                    throw new UnauthorizedAccessException();

                var base64 = authorization.Substring(6);
                var credentialBytes = Convert.FromBase64String(base64);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
                var username = credentials[0];
                var password = credentials[1];

                if ("j+dF48FhiU+Dz83ZQYsoXw==" != password)
                    throw new ApplicationException("Invalid password");

                if (year < 2017 || year > 2050)
                    throw new ApplicationException("Invalid year");

                // The only report we can return via API is 'summary'
                if ("summary" != topic)
                    throw new ApplicationException("Invalid topic");

                // The only constraint that's implemented is 'semimonthly'
                DateTime? beforedate = null;
                if (constraint?.ToLower() == "semimonthly")
                {
                    // Semimonthly constraint means return ONLY transactions before or equal to last payday,
                    // which is day before 1st and 16th.
                    var now = DateTime.Now;
                    int beforeday = (now.Day > 15) ? 16 : 1;
                    beforedate = new DateTime(now.Year, now.Month, beforeday);
                }

                Func<Models.Transaction, bool> inscope = (x => x.Timestamp.Year == year && x.Hidden != true);
                if (beforedate.HasValue)
                    inscope = (x => x.Timestamp < beforedate.Value && x.Timestamp.Year == year && x.Hidden != true); 

                var txs = _context.Transactions.Where(inscope).Where(x => x.Splits?.Any() != true);
                var splits = _context.Splits.Include(x => x.Transaction).Where(x => inscope(x.Transaction));
                var groupings = txs.AsParallel<ISubReportable>().Union(splits.AsParallel<ISubReportable>()).OrderBy(x => x.Timestamp).GroupBy(x => x.Timestamp.Month);

                var builder = new Helpers.ReportBuilder(_context);
                var report = await builder.ThreeLevelReport(groupings, true);
                var result = new ApiSummaryReportResult(report);

                // For the summary report via api, we don't want any Key1 blanks
                result.Lines.RemoveAll(x => x.Key1 == null); 

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
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
    }

    public class ApiResult
    {
        public bool Ok { get; set; } = true;

        public Exception Exception { get; set; } = null;

        public ApiResult() { }

        public ApiResult(Exception ex)
        {
            Exception = ex;
            Ok = false;
        }

        public static implicit operator string(ApiResult r) => JsonConvert.SerializeObject(r);
    }

    public class ApiTransactionResult : ApiResult
    {
        public Transaction Transaction { get; set; }

        public ApiTransactionResult(Transaction transaction)
        {
            Transaction = transaction;
        }
    }

    public class ApiPayeeResult : ApiResult
    {
        public Payee Payee { get; set; }

        public ApiPayeeResult(Payee payee)
        {
            Payee = payee;
        }
    }

    public class ApiSummaryReportResult : ApiResult
    {
        public struct Line
        {
            public string Category;
            public string SubCategory;
            public string Key1;
            public string Key2;
            public string Key3;
            public decimal Amount;
        }

        public List<Line> Lines = new List<Line>();

        public ApiSummaryReportResult(PivotTable<Label, Label, decimal> report)
        {
            foreach (var rowlabel in report.RowLabels)
            {
                var line = new Line();

                line.Category = rowlabel.Value;

                if (rowlabel.Emphasis)
                    line.SubCategory = "Total";
                else
                    line.SubCategory = rowlabel.SubValue;

                if (!string.IsNullOrEmpty(rowlabel.Key1))
                    line.Key1 = rowlabel.Key1;
                if (!string.IsNullOrEmpty(rowlabel.Key2))
                    line.Key2 = rowlabel.Key2;
                if (!string.IsNullOrEmpty(rowlabel.Key3))
                    line.Key3 = rowlabel.Key3;

                var column = report.Columns.Where(x => x.Value == "TOTAL").FirstOrDefault();

                if (null != column)
                {
                    var cell = report.Table[rowlabel][column];
                    line.Amount = cell;
                    Lines.Add(line);
                }
            }
        }
    }
}
