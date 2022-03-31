using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core.Repositories
{
    /// <summary>
    /// Contains ALL data repositories
    /// </summary>
    public class AllRepositories
    {
        public AllRepositories(ITransactionRepository t, IBudgetTxRepository b, IPayeeRepository p)
        {
            Transactions = t;
            BudgetTxs = b;
            Payees = p;
        }

        public ITransactionRepository Transactions { get; private set; }
        public IBudgetTxRepository BudgetTxs { get; private set; }
        public IPayeeRepository Payees { get; private set; }
    }
}
