using Common.NET;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.Extensions.Configuration;
using OfxSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Quieriers;
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
        public TransactionRepository(IDataContext context, IPlatformAzureStorage storage, IConfiguration config) : base(context)
        {
            _storage = storage;
            _config = config;
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
            var splitsquery = _context.Splits.Where(x => x.Transaction.Hidden != true);
            if (!allyears)
                splitsquery = splitsquery.Where(x => x.Transaction.Timestamp.Year == Year);
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

            string blobname = transaction.ID.ToString();

            _storage.Initialize();
            await _storage.UploadToBlob(BlobStoreName, blobname, stream, contenttype);

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

            var name = transaction.ID.ToString();

            // See Bug #991: Production bug: Receipts before 5/20/2021 don't download
            // If the ReceiptUrl contains an int value, use THAT for the blobname instead.

            if (Int32.TryParse(transaction.ReceiptUrl, out _))
                name = transaction.ReceiptUrl;

            _storage.Initialize();
            var stream = new MemoryStream();
            var contenttype = await _storage.DownloadBlob(BlobStoreName, name, stream);

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

        #endregion

        #region internals

        private readonly IPlatformAzureStorage _storage;
        private readonly IConfiguration _config;

        // TODO: This is dumb. _storage should know its own blobcontainer name
        private string BlobStoreName => _config["Storage:BlobContainerName"] ?? throw new ApplicationException("Must define a blob container name");

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
