using Common.NET;
using Excel.FinancialFunctions;
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
        public TransactionRepository(IDataContext context, IAsyncQueryExecution queryExecution, IStorageService storage = null) : base(context)
        {
            _queryExecution = queryExecution;
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
        // TODO: QueryExec SingleAsync()

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
        public async Task BulkEditAsync(string category)
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
        public async Task<Stream> AsSpreadsheetAsync(int Year, bool allyears, string q)
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
            var transactions = await _queryExecution.ToListNoTrackingAsync(transactionsdtoquery);

            // Which splits?

            // Product Backlog Item 870: Export & import transactions with splits
            var splitsquery = _context.Splits.Where(x => transactionsquery.Contains(x.Transaction)).OrderByDescending(x => x.Transaction.Timestamp);
            var splits = await _queryExecution.ToListNoTrackingAsync(splitsquery);

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

            return stream as Stream;
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

        public IEnumerable<Split> CalculateCustomSplitRules(Transaction transaction, string rule)
        {
            var result = new List<Split>();

            try
            {
                if (string.IsNullOrEmpty(rule))
                    throw new ArgumentNullException();

                // Here's how the Loan rule looks:
                // var rule = "Mortgage Principal [Loan] { \"interest\": \"Mortgage Interest\", \"amount\": 200000, \"rate\": 6, \"term\": 180, \"origination\": \"1/1/2000\" } ";

                var trimmed = rule.Trim();
                if (!trimmed.Contains("[Loan]"))
                    throw new ArgumentException();

                var split = trimmed.Split("[Loan]");
                if (split.Count() != 2)
                    throw new ArgumentException();

                var category = split[0].Trim();

                var loan = JsonSerializer.Deserialize<LoanDefinition>(split[1], new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                if (loan == null || loan.OriginationDate == default)
                    throw new ArgumentException();

                if (string.IsNullOrEmpty(loan.Principal))
                    loan.Principal = category;
                else if (string.IsNullOrEmpty(loan.Interest))
                    loan.Interest = category;

                // We have to recalculate the payment, we can't use the one that's send in. This is because the payment sent in is only prceise to two
                // decimal points. However, amortization tables are calculated based on payments to greater precision

                var pmt = Financial.Pmt(rate: loan.RatePctPerMo, nper: loan.Term, pv: loan.Amount, fv:0, typ: PaymentDue.EndOfPeriod);

                var paymentnum = transaction.Timestamp.Year * 12 + transaction.Timestamp.Month - loan.OriginationDate.Year * 12 - loan.OriginationDate.Month;
                var ipmt = (decimal)Math.Round(Financial.IPmt(rate: loan.RatePctPerMo, per: 1 + paymentnum, nper: loan.Term, pv: loan.Amount, fv: 0, typ: PaymentDue.EndOfPeriod), 2);

                var ppmt = transaction.Amount - ipmt;

                result.Add(new Split() { Amount = ipmt, Category = loan.Interest, Memo = "Auto calculated loan interest" });
                result.Add(new Split() { Amount = ppmt, Category = loan.Principal, Memo = "Auto calculated loan principal" });
            }
            catch
            {
                // Problems? Ignore. 
                // We'll just return an empty list
            }

            return result;
        }

        /// <summary>
        /// Give a category subtriung <paramref name="q"/> return all recent categories containing that
        /// </summary>
        /// <param name="q">Substring query</param>
        /// <returns>List of containing categories</returns>
        public async Task<IEnumerable<string>> CategoryAutocompleteAsync(string q)
        {
            if (string.IsNullOrEmpty(q))
                return Enumerable.Empty<String>();

            const int numresults = 10;

            // Look for top N recent categories in transactions, first.
            var txd = All.Where(x => x.Timestamp > DateTime.Now.AddMonths(-18) && x.Category.Contains(q)).GroupBy(x => x.Category).Select(g => new { Key = g.Key, Value = g.Count() }).OrderByDescending(x => x.Value).Take(numresults);

            // There are also some categories in splits. Get the top N there too.
            var spd = Splits.Where(x => x.Transaction.Timestamp > DateTime.Now.AddMonths(-18) && x.Category.Contains(q)).GroupBy(x => x.Category).Select(g => new { Key = g.Key, Value = g.Count() }).OrderByDescending(x => x.Value).Take(numresults);

            // Merge the results

            // https://stackoverflow.com/questions/2812545/how-do-i-sum-values-from-two-dictionaries-in-c
            var query = txd.Concat(spd).GroupBy(x => x.Key).Select(x => new { Key = x.Key, Value = x.Sum(g => g.Value) }).OrderByDescending(x => x.Value).Take(numresults).Select(x => x.Key);

            var result = await _queryExecution.ToListNoTrackingAsync(query);

            return result;

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

        private readonly IAsyncQueryExecution _queryExecution;

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
