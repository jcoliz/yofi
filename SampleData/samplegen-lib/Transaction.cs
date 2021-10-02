using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YoFi.SampleGen
{
    public class Transaction
    {
        public string Payee { get; set; }
        public DateTime Timestamp { get; set; }

        public string Category => Splits.First().Category;
        public decimal Amount => Splits.First().Amount;
        public decimal TotalAmount => Splits.Sum(x => x.Amount);

        public List<CategoryAmount> Splits = new List<CategoryAmount>() { new CategoryAmount() };
    }

    public class CategoryAmount
    {
        public string Category { get; set; }
        public decimal Amount { get; set; }
    }
}
