using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Models
{
    public class BudgetTx: IReportable, IID
    {
        public int ID { get; set; }
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Amount { get; set; }
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime Timestamp { get; set; }
        public string Category { get; set; }

        public BudgetTx() { }

        public BudgetTx(BudgetTx copy,DateTime dt)
        {
            Amount = copy.Amount;
            Category = copy.Category;
            Timestamp = dt;
        }

        public override bool Equals(object obj)
        {
            return obj is BudgetTx tx &&
                   Amount == tx.Amount &&
                   Timestamp == tx.Timestamp &&
                   Category == tx.Category;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Amount, Timestamp, Category);
        }
    }
}
