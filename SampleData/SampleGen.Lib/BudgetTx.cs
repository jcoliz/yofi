using System;
using System.Collections.Generic;
using System.Text;

namespace YoFi.SampleGen
{
    public class BudgetTx
    {
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string Category { get; set; }
    }
}
