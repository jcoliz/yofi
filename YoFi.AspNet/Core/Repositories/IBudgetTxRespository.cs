using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories.Wire;

namespace YoFi.Core.Repositories
{
    /// <summary>
    /// Provides access to Payee items, along with 
    /// domain-specific business logic unique to Payee items
    /// </summary>
    public interface IBudgetTxRepository : IWireRespository<BudgetTx>, IRepository<BudgetTx>
    {
        /// <summary>
        /// Remove all selected items from the database
        /// </summary>
        Task BulkDeleteAsync();
    }
}
