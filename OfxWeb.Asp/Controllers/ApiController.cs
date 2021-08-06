using Common.NET;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using OfxWeb.Asp.Controllers.Reports;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Controllers
{
    // Note that these methods largely do their own Json serialization. This
    // is currently needed because the front end expectes everything in PascalCase,
    // but the default formatter returns everything in camelCase. In later versions
    // of .NET core this is configurable. But for now, we have to do it ourselves.

    [Produces("application/json")]
    [Route("api/tx")]
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

        // GET: api/tx
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

        // GET: api/tx/5
        [HttpGet("{id}", Name = "Get")]
        public async Task<object> Get(int id)
        {
            try
            {
                return new ApiTransactionResult(await LookupTransactionAsync(id));
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        // GET: api/tx/ApplyPayee/5
        [HttpGet("ApplyPayee/{id}")]
        public async Task<object> ApplyPayee(int id)
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

                return new ApiPayeeResult(payee);
            }
            catch (Exception ex)
            {
                return new ApiResult(ex);
            }
        }

        // GET: api/tx/Hide/5
        [HttpGet("Hide/{id}")]
        public async Task<ApiResult> Hide(int id)
        {
            try
            {
                var transaction = await LookupTransactionAsync(id);

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
        public async Task<ApiResult> Show(int id)
        {
            try
            {
                var transaction = await LookupTransactionAsync(id);

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
        public async Task<ApiResult> Select(int id)
        {
            try
            {
                var transaction = await LookupTransactionAsync(id);

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
        public async Task<ApiResult> Deselect(int id)
        {
            try
            {
                var transaction = await LookupTransactionAsync(id);

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
        public async Task<ApiResult> SelectPayee(int id)
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
        public async Task<ApiResult> DeselectPayee(int id)
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
        public async Task<object> Edit(int id, bool? duplicate, [Bind("ID,Timestamp,Amount,Memo,Payee,Category,SubCategory,BankReference,ReceiptUrl")] Models.Transaction transaction)
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

        public async Task<object> UpSplits(int id, IFormFile file)
        {
            try
            {
                var transaction = await LookupTransactionAsync(id,splits:true);

                var incoming = new HashSet<Models.Split>();
                // Extract submitted file into a list objects

                if (file.FileName.ToLower().EndsWith(".xlsx"))
                {
                    using (var stream = file.OpenReadStream())
                    {
                        var excel = new ExcelPackage(stream);
                        var worksheet = excel.Workbook.Worksheets.Where(x => x.Name == "Splits").Single();
                        worksheet.ExtractInto(incoming);
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

                return new ApiTransactionResult(transaction);
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
        public async Task<object> EditPayee(int id, bool? duplicate, [Bind("ID,Name,Category,SubCategory")] Models.Payee payee)
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
        public async Task<object> AddPayee([Bind("Name,Category,SubCategory")] Payee payee)
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

        // GET: api/tx/reportv2/all
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

        // GET: api/tx/cat-ac
        [HttpGet("cat-ac")]
        public List<string> CategoryAutocomplete(string q)
        {
            var result = new List<string>() { "A:B:C:D", "E:F:G:H", "I:J:K:L", q };

            return result;
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
}
