using jcoliz.OfficeOpenXml.Serializer;
using OfxSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using Transaction = YoFi.Core.Models.Transaction;

namespace YoFi.Core.Importers
{
    /// <summary>
    /// Import transactions into a repository
    /// </summary>
    /// <remarks>
    /// Transactions get special handling, especially special duplicate handling. Also they can import OFX files
    /// </remarks>
    public class TransactionImporter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="repository">Where to store transactions</param>
        /// <param name="payees">Where to find payees for matching</param>
        public TransactionImporter(AllRepositories repos)
        {
            _repository = repos.Transactions;
            _payees = repos.Payees;
        }

        /// <summary>
        /// All the ids of transactions which should be highlighted to the user for further
        /// consideration
        /// </summary>
        public IEnumerable<int> HighlightIDs => highlights.Select(x => x.ID);

        /// <summary>
        /// Load transactions from an OFX file, and hold them in a queue for furhter processing
        /// </summary>
        /// <param name="stream">Source of data</param>
        public async Task QueueImportFromOfxAsync(Stream stream)
        {
            OfxDocument Document = await OfxDocumentReader.FromSgmlFileAsync(stream);

            var created = Document.Statements.SelectMany(x => x.Transactions).Select(
                tx => new Transaction()
                {
                    Amount = tx.Amount,
                    Payee = (tx.Name ?? tx.Memo) ?.Trim(),
                    BankReference = tx.ReferenceNumber?.Trim(),
                    Timestamp = tx.Date.Value.DateTime
                }
            );

            incoming.AddRange(created);
        }

        /// <summary>
        /// Load transactions from an XLSX file, and hold them in a queue for further processing
        /// </summary>
        /// <param name="reader">Source of data</param>
        public void QueueImportFromXlsx(ISpreadsheetReader reader)
        {
            var items = reader.Deserialize<Transaction>();
            incoming.AddRange(items);

            // If there are also splits included here, let's grab those
            // And transform the flat data into something easier to use.
            if (reader.SheetNames.Contains("Split"))
                splits.AddRange(reader.Deserialize<Split>()?.ToLookup(x => x.TransactionID));
        }


        /// <summary>
        /// Add the previously queued transactions to the database, in an 'imported' state
        /// </summary>
        public async Task ProcessImportAsync()
        {
            // Process needed changes on each
            foreach (var item in incoming)
            {
                // Default status for imported items
                item.Selected = true;
                item.Imported = true;
                item.Hidden = true;

                // Generate a bank reference if doesn't already exist
                if (string.IsNullOrEmpty(item.BankReference))
                    item.GenerateBankReference();
            }

            //
            // (2) Handle duplicates
            //

            // Deselect duplicate transactions. By default, deselected transactions will not be imported. User can override.

            // To handle the case where there may be transactions already in the system before the importer
            // assigned them a bankreference, we will assign bankreferences retroactively to any overlapping
            // transactions in the system.

            await _repository.AssignBankReferences();

            // Flag duplicate transactions. If there is an existing transaction with the same bank reference, we'll have to investigate further

            var highlightme = ManageConflictingImports(incoming);
            highlights.AddRange(highlightme);

            //
            // (3) Final processing on each transction
            //

            await _payees.LoadCacheAsync();

            // Process each item

            foreach (var item in incoming)
            {
                // (3A) Fixup and match payees

                item.Payee = item.StrippedPayee;
                if (string.IsNullOrEmpty(item.Category))
                {
                    var category = await _payees.GetCategoryMatchingPayeeAsync(item.Payee);

                    // Consider custom split rules based on matched category
                    var customsplits = _repository.CalculateCustomSplitRules(item, category);
                    if (customsplits.Any())
                        item.Splits = customsplits.ToList();
                    else
                        item.Category = category;
                }

                // (3B) Import splits
                // Product Backlog Item 870: Export & import transactions with splits

                var mysplits = splits.Where(x => x.Key == item.ID).SelectMany(x => x);
                if (mysplits.Any())
                {
                    item.Splits = mysplits.ToList();
                    item.Category = null;
                    foreach (var split in item.Splits)
                    {
                        // Clear any imported IDs
                        split.ID = 0;
                        split.TransactionID = 0;
                    }
                }

                // (3C) Clear any imported ID
                item.ID = 0;
            }

            // Add resulting transactions

            await _repository.BulkInsertAsync(incoming);
        }

        #region Internals

        /// <summary>
        /// Deal with conflicting transactions
        /// </summary>
        /// <remarks>
        /// For each incoming transaction, deselect it if there is already a transaction with a matching bankref in
        /// the database. If the transaction with a matching bankref doesn't exactly equal the considered transaction,
        /// it will be included in the returned tranasactions. These are the suspicious transactions that the user
        /// should look at more carefully.
        /// </remarks>
        private IEnumerable<Transaction> ManageConflictingImports(IEnumerable<Transaction> incoming)
        {
            var result = new List<Transaction>();

            // Flag duplicate transactions. If there is an existing transaction with the same bank reference, we'll have to investigate further

            // The approach is to create a lookup, from bankreference to a list of possible matching conflicts. Note that it is possible for multiple different
            // transactions to collide on a single hash. We will have to step through all the possible conflicts to see if there is really a match.

            // Note that this expression evaluates nicely into SQL. Nice job EF peeps!
            /*
                SELECT [x].[ID], [x].[AccountID], [x].[Amount], [x].[BankReference], [x].[Category], [x].[Hidden], [x].[Imported], [x].[Memo], [x].[Payee], [x].[ReceiptUrl], [x].[Selected], [x].[SubCategory], [x].[Timestamp]
                FROM [Transactions] AS [x]
                WHERE [x].[BankReference] IN (N'A1ABC7FE34871F02304982126CAF5C5C', N'EE49717DE89A3D97A9003230734A94B7')
                */
            //
            var uniqueids = incoming.Select(x => x.BankReference).ToHashSet();
            var conflicts = _repository.All.Where(x => uniqueids.Contains(x.BankReference)).ToLookup(x => x.BankReference, x => x);

            if (conflicts.Any())
            {
                foreach (var tx in incoming)
                {
                    // If this has any bank ID conflict, we are doing to deselect it. The BY FAR most common case of a
                    // Bankref collision is a duplicate transaction

                    if (conflicts[tx.BankReference].Any())
                    {
                        Console.WriteLine($"{tx.Payee} ({tx.BankReference}) has a conflict");

                        // Deselect the transaction. User will have a chance later to re-select it
                        tx.Selected = false;

                        // That said, there IS a chance that this is honestly a new transaction with a bankref collision.
                        // If we can't find the obvious collision, we'll flag it for the user to sort it out. Still, the
                        // most likely case is it's a legit duplicate but the user made slight changes to the payee or
                        // date.

                        if (!conflicts[tx.BankReference].Any(x => x.Equals(tx)))
                        {
                            Console.WriteLine($"Conflict may be a false positive, flagging for user.");
                            result.Add(tx);
                        }
                    }
                }
            }

            return result;
        }

        #endregion

        #region Fields

        /// <summary>
        /// Pending queue of transactions to import
        /// </summary>
        private readonly List<Transaction> incoming = new();

        /// <summary>
        /// Pending queue of splits to import
        /// </summary>
        private readonly List<IGrouping<int, Split>> splits = new();

        /// <summary>
        /// Probably duplicate items, but the user should check to be sure.
        /// </summary>
        private readonly List<Transaction> highlights = new();

        /// <summary>
        /// Target repository for storing the imported results
        /// </summary>
        private readonly ITransactionRepository _repository;

        /// <summary>
        /// Repository to use for payee matching
        /// </summary>
        private readonly IPayeeRepository _payees;

        #endregion

    }
}
