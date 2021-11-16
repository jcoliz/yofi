using System;

namespace YoFi.Core.Models
{
    public class Loan
    {
        public double Amount { get; set; }

        public double Rate { get; set; }

        public double RatePctPerMo => Rate / 100.0 / 12.0;

        public string Origination { get; set; }

        public DateTime OriginationDate => DateTime.Parse(Origination);

        public int Term { get; set; }

        public string Principal { get; set; }

        public string Interest { get; set; }
    }
}
