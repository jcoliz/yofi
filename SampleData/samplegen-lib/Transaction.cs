using System;
using System.Collections.Generic;
using System.Text;

namespace YoFi.SampleGen
{
    public class Transaction
    {
        public string Category { get; set; }
        public string Payee { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal Amount { get; set; }
    }
}
