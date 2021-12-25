using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.Core.Repositories
{
    /// <summary>
    /// Provides access to Payee items, along with 
    /// domain-specific business logic unique to Payee items
    /// </summary>
    public interface IBudgetTxRepository : IRepository<BudgetTx>
    {
        /// <summary>
        /// Remove all selected items from the database
        /// </summary>
        Task BulkDeleteAsync();
    }
}
