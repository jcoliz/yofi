using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.Core.Repositories
{
    /// <summary>
    /// Provides access to Payee items, along with 
    /// domain-specific business logic unique to Payee items
    /// </summary>
    public interface IPayeeRepository : IRepository<Payee>
    {
        /// <summary>
        /// Change category of all selected items to <paramref name="category"/>
        /// </summary>
        /// <param name="category">Next category</param>
        Task BulkEditAsync(string category);

        /// <summary>
        /// Remove all selected items from the database
        /// </summary>
        Task BulkDeleteAsync();

        /// <summary>
        /// Create a new payee based upon the supplied transaction (by id)
        /// </summary>
        /// <remarks>
        /// The idea is that the resulting payee matching rule would match the
        /// given transaction, and assign its current category.
        /// 
        /// Note that this doesn't persist the new item to storage. Only returns
        /// it for the caller to work on some more.
        /// </remarks>
        /// <param name="txid">Id of existing transaction</param>
        Task<Payee> NewFromTransactionAsync(int txid);

        Task LoadCacheAsync();

        /// <summary>
        /// Find the category which is the best match for the payee <paramref name="Name"/>
        /// </summary>
        /// <param name="Name">Name of payee to search for a match</param>
        /// <returns>Matching category or null if no match</returns>
        Task<string> GetCategoryMatchingPayeeAsync(string Name);
    }
}
