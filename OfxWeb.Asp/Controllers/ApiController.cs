using System;
using System.Collections.Generic;
using System.Linq;
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

        public ApiController(ApplicationDbContext context)
        {
            _context = context;
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

                IPlatformAzureStorage storage = new DotNetAzureStorage("DefaultEndpointsProtocol=http;AccountName=jcolizstorage;AccountKey=kjfiUJrgAq/FP0ZL3uVR9c5LPq5dI3MCfCNNnwFRDtrYs63FU654j4mBa4tmkLm331I4Xd/fhZgORnhkEfb4Eg==");
                storage.Initialize();

                string contenttype = null;

                using (var stream = file.OpenReadStream())
                {

                    // Upload the file
                    await storage.UploadToBlob(BlobStoreName, id.ToString(), stream, file.ContentType);

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


#if false
        // POST: api/Api
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }
        
        // PUT: api/Api/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }
        
        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
#endif
    }

    public class ApiResult
    {
        public bool Ok { get; } = true;

        public Exception Exception { get; } = null;

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
        public Transaction Transaction { get; }

        public ApiTransactionResult(Transaction tx)
        {
            Transaction = tx;
        }
    }

    public class ApiPayeeResult : ApiResult
    {
        public Payee Payee { get; }

        public ApiPayeeResult(Payee p)
        {
            Payee = p;
        }
    }
}
