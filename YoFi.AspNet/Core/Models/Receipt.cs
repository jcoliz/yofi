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
        public int ID { get; set ; }

        [Editable(true)]
        public string Name { get; set ; }

        [Editable(true)]
        public decimal Amount { get; set; }

        [Editable(true)]
        public string Memo { get; set; }

        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        [Column(TypeName = "date")]
        [Editable(true)]
        public DateTime Timestamp { get; set; }

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
