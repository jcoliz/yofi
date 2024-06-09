using Excel.FinancialFunctions;
using System;
using System.Collections.Generic;

namespace YoFi.Core.Models;

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

    public IDictionary<string, decimal> PaymentSplitsForDate(DateTime date)
    {
        var paymentnum = date.Year * 12 + date.Month - OriginationDate.Year * 12 - OriginationDate.Month;
        var ipmt = Financial.IPmt(rate: RatePctPerMo, per: 1 + paymentnum, nper: Term, pv: Amount, fv: 0, typ: PaymentDue.EndOfPeriod);
        var pmt = Financial.Pmt(rate: RatePctPerMo, nper: Term, pv: Amount, fv: 0, typ: PaymentDue.EndOfPeriod);

        return new Dictionary<string, decimal>()
        {
            { Interest, (decimal)Math.Round(ipmt,2) },
            { Principal, (decimal)Math.Round(pmt,2) - (decimal)Math.Round(ipmt,2) },
        };
    }
}
