using Common.DotNet;
using Common.DotNet;
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
using YoFi.Core.Repositories.Wire;
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
        public TransactionRepository(IDataContext context, IClock clock, IStorageService storage = null) : base(context)
        {
            _storage = storage;
            _clock = clock;
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
        protected override IQueryable<Transaction> ForQuery(string q)
        {
            var qbuilder = new TransactionsQueryBuilder(Transaction.InDefaultOrder(_context.TransactionsWithSplits),_clock);
            qbuilder.BuildForQ(q);
            return qbuilder.Query;
        }

        protected override IQueryable<Transaction> ForQuery(IWireQueryParameters parms)
        {
            var qbuilder = new TransactionsQueryBuilder(Transaction.InDefaultOrder(_context.TransactionsWithSplits),_clock);
            qbuilder.BuildForQ(parms.Query);
            qbuilder.ApplyOrderParameter(parms.Order);
            qbuilder.ApplyViewParameter(parms.View);
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

        /// <summary>
        /// Ensure all items have a bank reference
        /// </summary>
        /// <returns></returns>
        public async Task AssignBankReferences()
        {
            // To handle the case where there may be transactions already in the system before the importer
            // assigned them a bankreference, we will assign bankreferences retroactively to any overlapping
            // transactions in the system.

            var needbankrefs = All.Where(x => null == x.BankReference);
            var any = await _context.AnyAsync(needbankrefs);
            if (any)
            {
                foreach (var tx in needbankrefs)
                    tx.GenerateBankReference();

                await UpdateRangeAsync(needbankrefs);
            }
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
            var transactions = await _context.ToListNoTrackingAsync(transactionsdtoquery);

            // Which splits?

            // Product Backlog Item 870: Export & import transactions with splits
            var splitsquery = _context.Splits.Where(x => transactionsquery.Contains(x.Transaction)).OrderByDescending(x => x.Transaction.Timestamp);
            var splits = await _context.ToListNoTrackingAsync(splitsquery);

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
        public async Task FinalizeImportAsync()
        {
            var accepted = All.Where(x => x.Imported == true && x.Selected == true);
            await _context.BulkUpdateAsync(accepted, new Transaction() { Hidden = false, Imported = false, Selected = false }, new List<string>() { "Hidden", "Imported", "Selected" });

            var rejected = All.Where(x => x.Imported == true && x.Selected != true);
            await _context.BulkDeleteAsync(rejected);
        }

        /// <summary>
        /// Remove all imported items without touching the live data set
        /// </summary>
        public Task CancelImportAsync()
        {
            IQueryable<Transaction> allimported = OrderedQuery.Where(x => x.Imported == true);

            return _context.BulkDeleteAsync(allimported);
        }

        // Based on https://gist.github.com/pies/4166888

        public IEnumerable<Split> CalculateCustomSplitRules(Transaction transaction, string rule)
        {
            try
            {
                if (string.IsNullOrEmpty(rule))
                    throw new ArgumentNullException();

                //
                // Deserialize the loan object out of the rule as sent in
                //
                // Here's how the Loan rule looks:
                // var rule = "Mortgage Principal [Loan] { \"interest\": \"Mortgage Interest\", \"amount\": 200000, \"rate\": 6, \"term\": 180, \"origination\": \"1/1/2000\" } ";
                //

                var trimmed = rule.Trim();
                if (!trimmed.Contains("[Loan]"))
                    throw new ArgumentException();

                var split = trimmed.Split("[Loan]");
                if (split.Count() != 2)
                    throw new ArgumentException();

                var category = split[0].Trim();

                var loan = JsonSerializer.Deserialize<Loan>(split[1], new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                if (loan == null || loan.OriginationDate == default)
                    throw new ArgumentException();

                if (string.IsNullOrEmpty(loan.Principal))
                    loan.Principal = category;
                else if (string.IsNullOrEmpty(loan.Interest))
                    loan.Interest = category;

                //
                // Calculate the payment for the transaction date, and transform that into splits
                //

                return loan.PaymentSplitsForDate(transaction.Timestamp).Select(x => new Split() { Amount = x.Value, Category = x.Key });
            }
            catch
            {
                // Problems? Ignore. 
                // We'll just return an empty list

                return Enumerable.Empty<Split>();
            }
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

            try
            {
                const int numresults = 10;

                // Look for top N recent categories in transactions, first.
                var txd = All.Where(x => x.Timestamp > _clock.Now.AddMonths(-18) && x.Category.Contains(q)).GroupBy(x => x.Category).Select(g => new { Key = g.Key, Value = g.Count() }).OrderByDescending(x => x.Value).Take(numresults);

                // There are also some categories in splits. Get the top N there too.
                var spd = Splits.Where(x => x.Transaction.Timestamp > _clock.Now.AddMonths(-18) && x.Category.Contains(q)).GroupBy(x => x.Category).Select(g => new { Key = g.Key, Value = g.Count() }).OrderByDescending(x => x.Value).Take(numresults);

                // Merge the results

                // Bug AB#1251: Because of the latest index changes I can't concat transaction and split queries on the server side anymore. I have to do it on the client.
                var txresult = await _context.ToListNoTrackingAsync(txd.GroupBy(x => x.Key).Select(x => new { x.Key, Value = x.Sum(g => g.Value) }));
                var splitresult = await _context.ToListNoTrackingAsync(spd.GroupBy(x => x.Key).Select(x => new { x.Key, Value = x.Sum(g => g.Value) }));

                // Need a rather complex merge here because splits and transactions COULD have the same category, and those need to be summed here first
                var result = txresult.Concat(splitresult).ToLookup(x=>x.Key).Select(x=> new { x.Key, Value = x.Sum(y => y.Value) }).OrderByDescending(x => x.Value).Take(numresults).Select(x => x.Key).ToList();

                return result;
            }
            catch (Exception)
            {
                // TODO: I should log this
                return Enumerable.Empty<String>();
            }
        }
        #endregion

        #region Fields

        private readonly IStorageService _storage;
        private readonly IClock _clock;

        #endregion

        #region Internal Query-builder helpers



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
    }
}
