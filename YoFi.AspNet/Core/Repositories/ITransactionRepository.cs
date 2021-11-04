using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.Core.Repositories
{
    public interface ITransactionRepository: IRepository<Transaction>
    {
        public Task<Transaction> GetWithSplitsByIdAsync(int? id);
        Task<bool> AssignPayeeAsync(Transaction transaction);
    }
}
