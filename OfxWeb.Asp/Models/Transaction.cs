using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Models
{
    public class Transaction : ICatSubcat, ISubReportable, IID
    {
        public int ID { get; set; }
        [DisplayFormat(DataFormatString = "{0:C2}")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime Timestamp { get; set; }
        public string Memo { get; set; }
        public string Payee { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string BankReference { get; set; }
        public bool? Hidden { get; set; }
        public bool? Imported { get; set; }
        public bool? Selected { get; set; }
        public string ReceiptUrl { get; set; }

        public ICollection<Split> Splits { get; set; }

        public bool HasSplits => Splits?.Any() == true;
        public bool IsSplitsOK => !HasSplits || ( Splits.Select(x=>x.Amount).Sum() == Amount );

        string IReportable.Category
        {
            get
            {
                if (string.IsNullOrEmpty(SubCategory))
                    return Category;
                else
                    return $"{Category}:{SubCategory}";
            }
        }

        /// <summary>
        /// Remove all characters from payee which are not whitespace or alpha-numeric
        /// </summary>
        public void FixupPayee()
        {
            Regex rx = new Regex(@"[^\s\w\d]+");
            Payee = rx.Replace(Payee, new MatchEvaluator(x => string.Empty));
        }

        //
        // Feature #814: Remove duplicate transactions on import
        //
        // Transactions are substatially equal if they have the same Payee, Date, and Amount. They may still be duplicates in this case,
        // but the user has to decide. This accounts for the world I'm in now where the bank stopped giving me a unique bank reference
        // number :P
        //

        // Store the hashcode in the bank reference. This makes it easier to find the hashcodes in the database.
        public void GenerateBankReference()
        {
            var signature = $"/{Payee ?? "Null"}/{Amount:C2}/{Timestamp.Date.ToShortDateString()}";
            var buffer = UTF32Encoding.UTF32.GetBytes(signature);
            var hash = new System.Security.Cryptography.MD5CryptoServiceProvider().ComputeHash(buffer);
            var x = hash.Aggregate(new StringBuilder(), (sb, b) => sb.Append(b.ToString("X2")));

            BankReference = x.ToString();
        }

        public override bool Equals(object obj)
        {
            bool result = false;

            if (obj is Transaction)
            {
                var other = obj as Transaction;
                result = string.Equals(Payee, other.Payee) && Amount == other.Amount && Timestamp.Date == other.Timestamp.Date;
            }

            return result;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Payee, Amount, Timestamp.Date);
        }
    }
}
