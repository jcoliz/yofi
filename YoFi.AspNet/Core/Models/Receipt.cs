using Common.DotNet;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace YoFi.Core.Models
{
    /// <summary>
    /// An independent receipt, waiting to be matched with a transaction
    /// </summary>
    public class Receipt : IID
    {
        public int ID { get; set; }

        [Editable(true)]
        public string Name { get; set; }

        [Editable(true)]
        public decimal? Amount { get; set; }

        [Editable(true)]
        public string Memo { get; set; }

        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        [Column(TypeName = "date")]
        [Editable(true)]
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Name of the underlying file
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Set of potential matches
        /// </summary>
        public ICollection<Transaction> Matches { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Receipt receipt &&
                   Name == receipt.Name &&
                   Amount == receipt.Amount &&
                   Timestamp == receipt.Timestamp;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Amount, Timestamp);
        }

        public int MatchesTransaction(Transaction transaction)
        {
            //
            // What does it mean to "match" a transaction?
            //
            // For anything that we DO have set, an "exact" match is
            //  * Date +/- 2 weeks either way
            //  * Receipt name is an exact substr of transaction name
            //  * Amount exactly matches
            //
            // A "partial" match is a match where we WOULD HAVE matched
            // if one of our set items was unset.
            //
            // Therefore, this method will return the QUALITY of the
            // match. In the case of exact match, it is 100 for each
            // matching property minus one point for each day different
            //
            // For a partial match... Which is better? A 2-item
            // partial match or a 2-item exact match? Hard to say, so
            // I am going to treat them the SAME for now
            //
            // Also note that date ONLY is not a match
            //

            var result = 0;

            if (transaction is null)
                return 0;

            if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(transaction.Payee))
            {
                if (transaction.Payee.Contains(Name))
                    result += 100;
            }

            if (Amount.HasValue)
            {
                if (Amount.Value == transaction.Amount)
                    result += 100;
            }

            if (Timestamp.HasValue)
            {
                var margin = TimeSpan.FromDays(14);
                if (transaction.Timestamp > Timestamp.Value - margin && transaction.Timestamp < Timestamp.Value + margin && result > 0)
                    result += 100 - (int)Math.Abs((transaction.Timestamp - Timestamp.Value).TotalDays);
            }

            return result;
        }

        /// <summary>
        /// Constructs a query which can be used to narrow transactions to then consider individually
        /// </summary>
        /// <param name="initial"></param>
        /// <param name="receipts"></param>
        /// <returns></returns>
        public static IQueryable<Transaction> TransactionsForReceipts(IQueryable<Transaction> initial, IEnumerable<Receipt> receipts)
        {
            var result = initial;

            // Can narrow date range if all have dates
            if (receipts.All(x=>x.Timestamp.HasValue))
            {
                var margin = TimeSpan.FromDays(14);

                var from = receipts.Min(x => x.Timestamp.Value);
                var to = receipts.Max(x => x.Timestamp.Value);

                result = result.Where(x=>x.Timestamp >= from-margin && x.Timestamp <= to+margin);
            }

            // Can narrow amount if all have amounts
            if (receipts.All(x=>x.Amount.HasValue))
            {
                var amounts = receipts.Select(x=>x.Amount.Value).ToList();
                result = result.Where(x=>amounts.Contains(x.Amount));
            }

            return result;
        }

        /// <summary>
        /// Generate a new receipt based on this filenname
        /// </summary>
        /// <remarks>
        /// Examples:
        ///     {name}.pdf
        ///     ${amount}.pdf
        ///     {mm}-{dd}.pdf
        ///     ${amount} {mm}-{dd}.pdf
        ///     {name} ${amount}.pdf
        ///     {name} ${amount} {mm}-{dd}.pdf
        ///     {name} ${amount} {mm}-{dd} {memo}.pdf
        ///     ${amount} ({singlewordmemo}).pdf
        /// </remarks>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Receipt FromFilename(string filename, IClock clock)
        {
            var result = new Receipt() { Filename = filename };

            var given = Path.GetFileNameWithoutExtension(filename);

            // Break the given name down into components
            var words = given.Split(' ').ToList();

            // Set of currently unmatched words
            var unmatchedwords = new List<string>();
            var unmatchedterms = new Queue<string>();

            var amount_r = new Regex("^\\$([0-9]+(?:\\.[0-9][0-9])?)");
            var date_r = new Regex("^[0-9][0-9]?-[0-9][0-9]?$");
            var memo_r = new Regex("^\\((.+?)\\)$");
            foreach (var word in words)
            {
                var match = amount_r.Match(word);
                if (match.Success)
                {
                    result.Amount = decimal.Parse(match.Groups[1].Value);
                    if (unmatchedwords.Any())
                    {
                        unmatchedterms.Enqueue(String.Join(' ',unmatchedwords));
                        unmatchedwords.Clear();
                    }
                }
                else
                {
                    match = date_r.Match(word);
                    if (match.Success)
                    {
                        var parsed = DateTime.Parse(match.Groups[0].Value);
                        result.Timestamp = new DateTime(clock.Now.Year,parsed.Month,parsed.Day);
                        if (result.Timestamp > clock.Now)
                        {
                            result.Timestamp = new DateTime(clock.Now.Year - 1, parsed.Month, parsed.Day);
                        }
                        if (unmatchedwords.Any())
                        {
                            unmatchedterms.Enqueue(String.Join(' ', unmatchedwords));
                            unmatchedwords.Clear();
                        }
                    }
                    else
                    {
                        match = memo_r.Match(word);
                        if (match.Success)
                        {
                            result.Memo = match.Groups[1].Value;
                            if (unmatchedwords.Any())
                            {
                                unmatchedterms.Enqueue(String.Join(' ', unmatchedwords));
                                unmatchedwords.Clear();
                            }
                        }
                        else
                        {
                            unmatchedwords.Add(word);
                        }
                    }
                }
            }
            if (unmatchedwords.Any())
            {
                unmatchedterms.Enqueue(String.Join(' ', unmatchedwords));
                unmatchedwords.Clear();
            }

            // Assign name based on the first unmatched items
            if (unmatchedterms.Any())
                result.Name = unmatchedterms.Dequeue();

            // Assign memo based on the second unmatched items
            if (unmatchedterms.Any() && result.Memo is null)
                result.Memo = unmatchedterms.Dequeue();

            return result;
        }
    }
}
