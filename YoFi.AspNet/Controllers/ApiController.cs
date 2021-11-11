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
        public IActionResult ReportV2([Bind("id,year,month,showmonths,level")] ReportParameters parms, [FromServices] IReportEngine reports)
        {
            var result = reports.Build(parms);
            var json = result.ToJson();

            return Content(json,"application/json");
        }

        [HttpGet("txi")]
        [ApiBasicAuthorization]
        public async Task<IActionResult> GetTransactions(string q = null)
        {
            var qbuilder = new TransactionsQueryBuilder(_context.Transactions);
            qbuilder.Build(q);

            return new JsonResult(await qbuilder.Query.ToListAsync());
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
        public async Task<IActionResult> ClearTestData(string id)
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
