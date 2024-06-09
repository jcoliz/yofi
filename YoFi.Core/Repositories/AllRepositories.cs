namespace YoFi.Core.Repositories;

/// <summary>
/// Contains ALL data repositories
/// </summary>
public class AllRepositories
{
    public AllRepositories(ITransactionRepository t, IBudgetTxRepository b, IPayeeRepository p, IReceiptRepository r)
    {
        Transactions = t;
        BudgetTxs = b;
        Payees = p;
        Receipts = r;
    }

    public ITransactionRepository Transactions { get; private set; }
    public IBudgetTxRepository BudgetTxs { get; private set; }
    public IPayeeRepository Payees { get; private set; }
    public IReceiptRepository Receipts { get; private set; }
}
