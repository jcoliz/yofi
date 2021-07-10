using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Models
{
    public class BudgetTx: IReportable, IID
    {
        public int ID { get; set; }
        [DisplayFormat(DataFormatString = "{0:C2}")]
        [Column(TypeName = "decimal(18,2)")]
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
                   Timestamp == tx.Timestamp &&
                   Category == tx.Category;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Timestamp, Category);
        }
    }
}
