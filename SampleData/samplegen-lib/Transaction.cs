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

        public string Category => (Splits.Count == 1) ? Splits.First().Category : string.Empty;
        public decimal Amount => Splits.Sum(x => x.Amount);
        public bool HasMultipleSplits => Splits.Count > 1;

        public List<CategoryAmount> Splits = new List<CategoryAmount>() { new CategoryAmount() };
    }

    public class CategoryAmount
    {
        public string Category { get; set; }
        public decimal Amount { get; set; }
    }
}
