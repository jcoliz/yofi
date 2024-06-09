using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.Core.Repositories;

/// <summary>
/// Provides access to Transaction items, along with 
/// domain-specific business logic specific to Transactions
/// </summary>
public interface ITransactionRepository: IRepository<Transaction>
{

    /// <summary>
    /// Create a new transaction
    /// </summary>
    /// <remarks>
    /// Note that this does not commit it to storage. Returns it for user to modify and then commit.
    /// </remarks>
    /// <returns>New empty transaction</returns>
    Task<Transaction> CreateAsync();

    /// <summary>
    /// Retrieve a single item by <paramref name="id"/>, including children splits
    /// </summary>
    /// <remarks>
    /// Will throw an exception if not found
    /// </remarks>
    /// <param name="id">Identifier of desired item</param>
    /// <returns>Desired item</returns>
    Task<Transaction> GetWithSplitsByIdAsync(int? id);

    /// <summary>
    /// Retrieve a single item by <paramref name="id"/>, including children splits, with a matching
    /// category based on payee if possible
    /// </summary>
    /// <remarks>
    /// Will throw an exception if not found.
    /// Note that the matched category is not saved. This is an edit-in-progress transaction 
    /// offered to the user to later commit
    /// </remarks>
    /// <param name="id">Identifier of desired item</param>
    /// <returns>Desired item with matched payee AND true if a category was automatically assigned</returns>
    Task<(Transaction,bool)> GetWithSplitsAndMatchCategoryByIdAsync(int? id);

    /// <summary>
    /// All splits including transactions
    /// </summary>
    IQueryable<Split> Splits { get; }

    /// <summary>
    /// Update only editable values from given transaction
    /// </summary>
    /// <remarks>
    /// TODO: Should use a DTO
    /// </remarks>
    /// <param name="id">ID of target transaction</param>
    /// <param name="newvalues">Item containing new values</param>
    /// <returns>Resulting edited transaction</returns>
    Task<Transaction> EditAsync(int id, Transaction newvalues);

    /// <summary>
    /// Create a new split and add it to transaction #<paramref name="id"/>
    /// </summary>
    /// <param name="id">ID of target transaction</param>
    /// <returns>ID of resulting split</returns>
    Task<int> AddSplitToAsync(int id);

    /// <summary>
    /// Remove the given split from its parent transaction
    /// </summary>
    /// <param name="id">ID of split</param>
    /// <returns>Transaction ID of parent transaction</returns>
    Task<int> RemoveSplitAsync(int id);

    /// <summary>
    /// Change category of all selected items to <paramref name="category"/>
    /// </summary>
    /// <param name="category">Next category</param>
    Task BulkEditAsync(string category);

    /// <summary>
    /// Export all items to a spreadsheet, in default order
    /// </summary>
    /// <returns>Stream containing the spreadsheet file</returns>
    Task<Stream> AsSpreadsheetAsync(int year, bool allyears, string q);

    /// <summary>
    /// Upload a receipt to blob storage and save the location to this <paramref name="transaction"/>
    /// </summary>
    /// <param name="transaction">Which transaction this is for</param>
    /// <param name="stream">Source location of receipt file</param>
    /// <param name="contenttype">Content type of this file</param>
    Task UploadReceiptAsync(Transaction transaction, Stream stream, string contenttype);

    /// <summary>
    /// Upload a receipt to blob storage and save the location to this <paramref name="transaction"/>
    /// </summary>
    /// <param name="id">Transaction ID</param>
    /// <param name="stream">Source location of receipt file</param>
    /// <param name="contenttype">Content type of this file</param>
    Task UploadReceiptAsync(int id, Stream stream, string contenttype);

    /// <summary>
    /// Get a receipt from storage
    /// </summary>
    /// <param name="transaction">Which transaction this is for</param>
    /// <returns>Tuple containing: 'stream' where to find the file, 'contenttype' the type of the data, and 'name' the suggested filename</returns>
    Task<(Stream stream, string contenttype, string name)> GetReceiptAsync(Transaction transaction);

    /// <summary>
    /// Remove the a receipt associated with this transaction
    /// </summary>
    /// <param name="id">Transaction ID</param>
    Task DeleteReceiptAsync(int id);

    /// <summary>
    /// Finally merge in all selected imported items into the live data set
    /// </summary>
    Task FinalizeImportAsync();

    /// <summary>
    /// Remove all imported items without touching the live data set
    /// </summary>
    Task CancelImportAsync();

    /// <summary>
    /// Using the rules listed in the supplied <paramref name="json"/>, calculate splits for this
    /// <paramref name="transaction"/>.
    /// </summary>
    /// <remarks>
    /// Currently this is only implemented for splitting loan principal and interest,
    /// but could be extended to others.
    /// </remarks>
    /// <param name="transaction">Transaction being considered</param>
    /// <param name="json">Rules to apply</param>
    /// <returns>New splits based on the rules</returns>
    IEnumerable<Split> CalculateCustomSplitRules(Transaction transaction, string json);

    /// <summary>
    /// Give a category subtriung <paramref name="q"/> return all recent categories containing that
    /// </summary>
    /// <param name="q">Substring query</param>
    /// <returns>List of containing categories</returns>
    Task<IEnumerable<string>> CategoryAutocompleteAsync(string q);

    /// <summary>
    /// Ensure all transactions have a bank reference
    /// </summary>
    /// <returns></returns>
    Task AssignBankReferences();

    /// <summary>
    /// Set the selected value on the given tx
    /// </summary>
    /// <param name="id">Transaction ID</param>
    /// <param name="value">New value</param>
    Task SetSelectedAsync(int id, bool value);

    /// <summary>
    /// Set the hidden value on the given tx
    /// </summary>
    /// <param name="id">Transaction ID</param>
    /// <param name="value">New value</param>
    Task SetHiddenAsync(int id, bool value);

    /// <summary>
    /// Using the payee which matches this transaction, update the category to match it
    /// </summary>
    /// <param name="id">Transaction ID</param>
    /// <returns>The category which was applied, or null if none matched</returns>
    Task<string> ApplyPayeeAsync(int id);
}
