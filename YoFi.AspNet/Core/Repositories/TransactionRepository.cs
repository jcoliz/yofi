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
    public class TransactionRepository : BaseRepository<Transaction>, ITransactionRepository
    {
        private readonly IPlatformAzureStorage _storage;
        private readonly IConfiguration _config;

        private readonly List<Transaction> incoming = new List<Transaction>();
        private readonly List<IGrouping<int, Split>> splits = new List<IGrouping<int, Split>>();
        private readonly List<Transaction> highlights = new List<Transaction>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">Where to find the data we actually contain</param>
        public TransactionRepository(IDataContext context, IPlatformAzureStorage storage, IConfiguration config) : base(context)
        {
            _storage = storage;
            _config = config;
        }

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

        public override IQueryable<Transaction> ForQuery(string q)
        {
            var qbuilder = new TransactionsQueryBuilder(Transaction.InDefaultOrder(_context.TransactionsWithSplits));
            qbuilder.Build(q);
            return qbuilder.Query;
        }

        // TODO: SingleAsync()
        public Task<Transaction> GetWithSplitsByIdAsync(int? id) => Task.FromResult(_context.TransactionsWithSplits.Single(x => x.ID == id.Value));

        public Task<Stream> AsSpreadsheet(int Year, bool allyears, string q)
        {
            // Which transactions?

            var qbuilder = new TransactionsQueryBuilder(_context.TransactionsWithSplits);
            qbuilder.Build(q);
            var transactionsquery = qbuilder.Query;

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

        #region Receipts

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

#region Importer

#if false
        public new void QueueImportFromXlsx(Stream stream)
        {
            using var ssr = new SpreadsheetReader();
            ssr.Open(stream);
            var items = ssr.Deserialize<Transaction>();
            incoming.AddRange(items);

            // If there are also splits included here, let's grab those
            // And transform the flat data into something easier to use.
            if (ssr.SheetNames.Contains("Split"))
                splits.AddRange(ssr.Deserialize<Split>()?.ToLookup(x => x.TransactionID));
        }

        public async Task QueueImportFromOfxAsync(Stream stream)
        {
            OfxDocument Document = await OfxDocumentReader.FromSgmlFileAsync(stream);

            var created = Document.Statements.SelectMany(x => x.Transactions).Select(
                tx => new Transaction()
                {
                    Amount = tx.Amount,
                    Payee = tx.Memo?.Trim(),
                    BankReference = tx.ReferenceNumber?.Trim(),
                    Timestamp = tx.Date.Value.DateTime
                }
            );

            incoming.AddRange(created);
        }
#endif
        public Task FinalizeImportAsync()
        {
            IQueryable<Transaction> allimported = OrderedQuery.Where(x => x.Imported == true);

            var selected = allimported.ToLookup(x => x.Selected == true);
            foreach (var item in selected[true])
                item.Imported = item.Hidden = item.Selected = false;

            // This will implicitly save the changes from the previous line
            return RemoveRangeAsync(selected[false]);
        }

        public Task CancelImportAsync()
        {
            IQueryable<Transaction> allimported = OrderedQuery.Where(x => x.Imported == true);
            return RemoveRangeAsync(allimported);
        }

#endregion

        private string BlobStoreName => _config["Storage:BlobContainerName"] ?? throw new ApplicationException("Must define a blob container name");

#region Data Transfer Objects

        /// <summary>
        /// The transaction data for export
        /// </summary>
        class TransactionExportDto : ICategory
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
