using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Models
{
    public class Transaction: ISubReportable
    {
        public int ID { get; set; }
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Amount { get; set; }
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime Timestamp { get; set; }
        public string Memo { get; set; }
        public string Payee { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string BankReference { get; set; }

        public ICollection<Split> Splits { get; set; }

        /// <summary>
        /// Remove all characters from payee which are not whitespace or alpha-numeric
        /// </summary>
        public void FixupPayee()
        {
            Regex rx = new Regex(@"[^\s\w\d]+");
            Payee = rx.Replace(Payee, new MatchEvaluator(x => string.Empty));
        }

        public override bool Equals(object obj)
        {
            return obj is Transaction && !string.IsNullOrEmpty(BankReference) && !string.IsNullOrEmpty(((Transaction)obj).BankReference) && ((Transaction)obj).BankReference == BankReference;
        }

        public override int GetHashCode()
        {
            return string.IsNullOrEmpty(BankReference) ? base.GetHashCode() : BankReference.GetHashCode();
        }
    }
}
