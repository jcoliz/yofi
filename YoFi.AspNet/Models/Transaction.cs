using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YoFi.AspNet.Models
{
    /// <summary>
    /// A single financial transaction
    /// </summary>
    /// <remarks>
    /// The basic building blocks!
    /// </remarks>
    public class Transaction : ICatSubcat, IID, IReportable
    {
        /// <summary>
        /// Object identity in Entity Framework
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Time transaction was created originally
        /// </summary>
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Who got this money, or gave it to use?
        /// </summary>
        public string Payee { get; set; }

        /// <summary>
        /// How much money are we talking here, anyway?
        /// </summary>
        [DisplayFormat(DataFormatString = "{0:C2}")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Cagtegorization of this transaction
        /// </summary>
        /// <remarks>
        /// Separate successive levels of depth with a colon, e.g. "Housing:Mortgage"
        /// </remarks>
        public string Category { get; set; }

        /// <summary>
        /// Second-level category
        /// </summary>
        /// <remarks>
        /// This property is obsolete. Category can now take multiple
        /// levels of information separated with colons.
        /// </remarks>
        public string SubCategory { get; set; }

        /// <summary>
        /// Optional commentary about this transaction
        /// </summary>
        public string Memo { get; set; }

        /// <summary>
        /// Bank-assigned unique ID
        /// </summary>
        /// <remarks>
        /// If a unique ID is not assigned by the bank, we'll add one on
        /// import.
        /// </remarks>
        public string BankReference { get; set; }

        /// <summary>
        /// Whether to hide this transaction from all views and calculations
        /// </summary>
        public bool? Hidden { get; set; }

        /// <summary>
        /// Whether this transaction was recently imported
        /// </summary>
        /// <remarks>
        /// And so thus should show up on the "Imported" page
        /// </remarks>
        public bool? Imported { get; set; }

        /// <summary>
        /// Whether this object will be included in the next bulk operation
        /// </summary>
        public bool? Selected { get; set; }

        /// <summary>
        /// The URL to a receipt image
        /// </summary>
        /// <remarks>
        /// This is no longer stored as a URL. It should be reformed to "bool HasReceipt"
        /// </remarks>
        public string ReceiptUrl { get; set; }

        /// <summary>
        /// For transactions with multiple categories, the detail on how much $$ goes in
        /// each category
        /// </summary>
        public ICollection<Split> Splits { get; set; }

        /// <summary>
        /// Does this transaction have any splits?
        /// </summary>
        public bool HasSplits => Splits?.Any() == true;

        /// <summary>
        /// Are all the splits fully balanced?
        /// </summary>
        public bool IsSplitsOK => !HasSplits || ( Splits.Select(x=>x.Amount).Sum() == Amount );

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
        
        /// <summary>
        /// Generate our own quasi-unique ID for this transaction
        /// </summary>
        /// <remarks>
        /// Used if it doesn't already have one.
        /// </remarks>
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
