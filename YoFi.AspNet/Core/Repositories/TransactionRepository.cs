using Common.NET;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.Core.Models;
using Transaction = YoFi.Core.Models.Transaction;

namespace YoFi.Core.Repositories
{
    /// <summary>
    /// Provides access to Transaction items, along with 
    /// domain-specific business logic specific to Transactions
    /// </summary>
    public class TransactionRepository : BaseRepository<Transaction>, ITransactionRepository
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">Where to find the data we actually contain</param>
        /// <param name="storage">Where to store receipts</param>
        /// <param name="config">Where to get configuration information</param>
        public TransactionRepository(IDataContext context, IStorageService storage = null) : base(context)
        {
            _storage = storage;
        }

        #region Read

        /// <summary>
        /// Subset of all known items reduced by the specified query parameter
        /// </summary>
        /// <remarks>
        /// For tranactions, this is a lot of work. Ergo, I made an entirely separate class, TransactionsQueryBuilder
        /// to handle this.
        /// </remarks>
        /// <param name="q">Query describing the desired subset</param>
        /// <returns>Query of requested items</returns>
        public override IQueryable<Transaction> ForQuery(string q)
        {
            var qbuilder = new TransactionsQueryBuilder(Transaction.InDefaultOrder(_context.TransactionsWithSplits));
            qbuilder.Build(q);
            return qbuilder.Query;
        }

        /// <summary>
        /// Retrieve a single item by <paramref name="id"/>, including children splits
        /// </summary>
        /// <remarks>
        /// Will throw an exception if not found
        /// </remarks>
        /// <param name="id">Identifier of desired item</param>
        /// <returns>Desired item</returns>
        public Task<Transaction> GetWithSplitsByIdAsync(int? id) => Task.FromResult(_context.TransactionsWithSplits.Single(x => x.ID == id.Value));
        // TODO: SingleAsync()

        /// <summary>
        /// All splits including transactions
        /// </summary>
        public IQueryable<Split> Splits => _context.SplitsWithTransactions;

        #endregion

        #region Update

        /// <summary>
        /// Change category of all selected items to <paramref name="category"/>
        /// </summary>
        /// <param name="category">Next category</param>
        public async Task BulkEdit(string category)
        {
            foreach (var item in All.Where(x => x.Selected == true))
            {
                item.Selected = false;

                if (!string.IsNullOrEmpty(category))
                {
                    // This may be a pattern-matching search, treat it like one
                    // Note that you can treat a non-pattern-matching replacement JUST LIKE a pattern
                    // matching one, it's just slower.
                    if (category.Contains("("))
                    {
                        var originals = item.Category?.Split(":") ?? default;
                        var result = new List<string>();
                        foreach (var component in category.Split(":"))
                        {
                            if (component.StartsWith("(") && component.EndsWith("+)"))
                            {
                                if (Int32.TryParse(component[1..^2], out var position))
                                    if (originals.Count() >= position)
                                        result.AddRange(originals.Skip(position - 1));
                            }
                            else if (component.StartsWith("(") && component.EndsWith(")"))
                            {
                                if (Int32.TryParse(component[1..^1], out var position))
                                    if (originals.Count() >= position)
                                        result.AddRange(originals.Skip(position - 1).Take(1));
                            }
                            else
                                result.Add(component);
                        }

                        if (result.Any())
                            item.Category = string.Join(":", result);
                    }
                    // It's just a simple replacement
                    else
                    {
                        item.Category = category;
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Create a new split and add it to transaction #<paramref name="id"/>
        /// </summary>
        /// <param name="id">ID of target transaction</param>
        /// <returns>ID of resulting split</returns>
        public async Task<int> AddSplitToAsync(int id)
        {
            var transaction = await GetWithSplitsByIdAsync(id);
            var result = new Split() { Category = transaction.Category };

            // Calculate the amount based on how much is remaining.

            var currentamount = transaction.Splits.Select(x => x.Amount).Sum();
            var remaining = transaction.Amount - currentamount;
            result.Amount = remaining;

            transaction.Splits.Add(result);

            // Remove the category information, that's now contained in the splits.

            transaction.Category = null;

            await UpdateAsync(transaction);

            return result.ID;
        }

        #endregion

        #region Export

        /// <summary>
        /// Export all items to a spreadsheet, in default order
        /// </summary>
        /// <returns>Stream containing the spreadsheet file</returns>
        public Task<Stream> AsSpreadsheet(int Year, bool allyears, string q)
        {
            var transactionsquery = ForQuery(q);

            transactionsquery = transactionsquery.Where(x => x.Hidden != true);
            if (!allyears)
                transactionsquery = transactionsquery.Where(x => x.Timestamp.Year == Year);
            transactionsquery = transactionsquery
                .OrderByDescending(x => x.Timestamp);

            // Select to data transfer object
            var transactionsdtoquery = transactionsquery
                .Select(x => new TransactionExportDto()
                {
                    ID = x.ID,
                    Amount = x.Amount,
                    Timestamp = x.Timestamp,
                    Category = x.Category,
                    Payee = x.Payee,
                    Memo = x.Memo,
                    ReceiptUrl = x.ReceiptUrl,
                    BankReference = x.BankReference
                }
                );

            // TODO: ToListAsync()
            var transactions = transactionsdtoquery.ToList();

            // Which splits?

            // Product Backlog Item 870: Export & import transactions with splits
            var splitsquery = _context.Splits.Where(x => transactionsquery.Contains(x.Transaction));
            splitsquery = splitsquery.OrderByDescending(x => x.Transaction.Timestamp);

            // TODO: ToListAsync()
            var splits = splitsquery.ToList();

            // Create the spreadsheet result

            var stream = new MemoryStream();
            using (var ssw = new SpreadsheetWriter())
            {
                ssw.Open(stream);
                ssw.Serialize(transactions, sheetname: nameof(Transaction));

                if (splits.Any())
                    ssw.Serialize(splits);
            }

            // Return it to caller

            stream.Seek(0, SeekOrigin.Begin);

            return Task.FromResult(stream as Stream);
        }

        #endregion

        #region Receipts

        /// <summary>
        /// Upload a receipt to blob storage and save the location to this <paramref name="transaction"/>
        /// </summary>
        /// <param name="transaction">Which transaction this is for</param>
        /// <param name="stream">Source location of receipt file</param>
        /// <param name="contenttype">Content type of this file</param>
        public async Task UploadReceiptAsync(Transaction transaction, Stream stream, string contenttype)
        {
            //
            // Save the file to blob storage
            //
            // TODO: Consolodate this with the exact same copy which is in ApiController
            //

            // Note that the view should not ever get this far. It's the view's reposibility to check first if
            // there is storage defined. Ergo, if we get this far, it's a legit 500 error.
            if (null == _storage)
                throw new ApplicationException("Storage is not defined");

            string blobname = transaction.ID.ToString();

            await _storage.UploadBlobAsync(blobname, stream, contenttype);

            // Save it in the Transaction
            // If there was a problem, UploadToBlob will throw an exception.

            transaction.ReceiptUrl = blobname;
            await UpdateAsync(transaction);
        }

        /// <summary>
        /// Get a receipt from storage
        /// </summary>
        /// <param name="transaction">Which transaction this is for</param>
        /// <returns>Tuple containing: 'stream' where to find the file, 'contenttype' the type of the data, and 'name' the suggested filename</returns>
        public async Task<(Stream stream, string contenttype, string name)> GetReceiptAsync(Transaction transaction)
        {
            if (string.IsNullOrEmpty(transaction.ReceiptUrl))
                return (null,null,null);

            // Note that the view should not ever get this far. It's the view's reposibility to check first if
            // there is storage defined. Ergo, if we get this far, it's a legit 500 error.
            if (null == _storage)
                throw new ApplicationException("Storage is not defined");

            var name = transaction.ID.ToString();

            // See Bug #991: Production bug: Receipts before 5/20/2021 don't download
            // If the ReceiptUrl contains an int value, use THAT for the blobname instead.

            if (Int32.TryParse(transaction.ReceiptUrl, out _))
                name = transaction.ReceiptUrl;

            var stream = new MemoryStream();
            var contenttype = await _storage.DownloadBlobAsync(name, stream);

            // Work around previous versions which did NOT store content type in blob store.
            if ("application/octet-stream" == contenttype)
                contenttype = "application/pdf";

            stream.Seek(0, SeekOrigin.Begin);

            return (stream,contenttype,name);
        }

        #endregion

        #region Import

        /// <summary>
        /// Finally merge in all selected imported items into the live data set
        /// </summary>
        public Task FinalizeImportAsync()
        {
            IQueryable<Transaction> allimported = OrderedQuery.Where(x => x.Imported == true);

            var selected = allimported.ToLookup(x => x.Selected == true);
            foreach (var item in selected[true])
                item.Imported = item.Hidden = item.Selected = false;

            // This will implicitly save the changes from the previous line
            return RemoveRangeAsync(selected[false]);
        }

        /// <summary>
        /// Remove all imported items without touching the live data set
        /// </summary>
        public Task CancelImportAsync()
        {
            IQueryable<Transaction> allimported = OrderedQuery.Where(x => x.Imported == true);
            return RemoveRangeAsync(allimported);
        }

        // Based on https://gist.github.com/pies/4166888

        public IEnumerable<Split> CalculateCustomSplitRules(Transaction transaction, string json)
        {
            var result = new List<Split>();

            var trimmed = json.Trim();
            if (trimmed[0] == '{' && trimmed[^1] == '}')
            {
                try
                {
                    var document = JsonDocument.Parse(trimmed);
                    if (document.RootElement.TryGetProperty("loan",out var element))
                    {
                        // There is a loan, now deserialize into a loan object
                        var thisjson = element.GetRawText();

                        var loan = JsonSerializer.Deserialize<LoanDefinition>(thisjson, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                        if (loan != null && loan.OriginationDate != default)
                        {
                            // We have to recalculate the payment, we can't use the one that's send in. This is because the payment sent in is only prceise to two
                            // decimal points. However, amortization tables are calculated based on payments to greater precision

                            // var pvif = Math.pow(1 + rate, nper);
                            // var pmt = rate / (pvif - 1) * -(pv * pvif + fv);
                            var pvif = Math.Pow(1 + loan.RatePctPerMo, loan.Term);
                            var pmt = -loan.RatePctPerMo / (pvif - 1) * loan.Amount * pvif;

                            // TMP = POWER(1+InterestRate/PaymentsPerYear,PaymentSchedule[@[PMT NO]]-1)
                            var paymentnum = transaction.Timestamp.Year * 12 + transaction.Timestamp.Month - loan.OriginationDate.Year * 12 - loan.OriginationDate.Month;
                            double factor = Math.Pow(1.0 + loan.RatePctPerMo, paymentnum);

                            // IPMT = PaymentSchedule[@[TOTAL PAYMENT]]*(M86-1)-LoanAmount*M86*(InterestRate/PaymentsPerYear)
                            var term1 = pmt * (factor - 1);
                            var term2 = loan.Amount * factor * loan.RatePctPerMo;
                            var ipmtd = -term1 - term2;
                            var ipmt = (decimal)Math.Round(ipmtd, 2);

                            var ppmt = transaction.Amount - ipmt;

                            result.Add(new Split() { Amount = ipmt, Category = loan.Interest, Memo = "Auto calculated loan interest" });
                            result.Add(new Split() { Amount = ppmt, Category = loan.Principal, Memo = "Auto calculated loan principal" });
                        }
                    }
                }
                catch
                {
                    // Problems? Ignore
                }
            }

            return result;
        }

        #endregion

        #region internals

        private readonly IStorageService _storage;

        #endregion

        #region Data Transfer Objects

        /// <summary>
        /// The shape of transactions when excported to spreadsheet
        /// </summary>
        class TransactionExportDto 
        {
            public int ID { get; set; }
            public DateTime Timestamp { get; set; }
            public string Payee { get; set; }
            public decimal Amount { get; set; }
            public string Category { get; set; }
            public string Memo { get; set; }
            public string BankReference { get; set; }
            public string ReceiptUrl { get; set; }
        }

        #endregion
    
        class LoanDefinition
        {          
            public double Amount { get; set; }

            public double Rate { get; set; }

            public double RatePctPerMo => Rate / 100.0 / 12.0;

            public string Origination { get; set; }

            public DateTime OriginationDate => DateTime.Parse(Origination);

            public int Term { get; set; }

            public string Principal { get; set; }

            public string Interest { get; set; }
        }
    }
}
