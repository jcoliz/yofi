using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Receipt FromFilename(string filename)
        {
            var result = new Receipt() { Filename = filename };

            var given = Path.GetFileNameWithoutExtension(filename);

            // Break the given name down into components
            var words = given.Split(' ').ToList();

            var amount_r = new Regex("^\\$([0-9]+(?:\\.[0-9][0-9])?)");
            var date_r = new Regex("^[0-9][0-9]?-[0-9][0-9]?$");
            for (int i = 0; i < words.Count; i++)
            {
                var word = words[i];
                var match = amount_r.Match(word);
                if (match.Success)
                {
                    result.Amount = decimal.Parse(match.Groups[1].Value);
                    words.RemoveAt(i);
                    i--;
                }
                match = date_r.Match(word);
                if (match.Success)
                {
                    result.Timestamp = DateTime.Parse(match.Groups[0].Value);
                    words.RemoveAt(i);
                    i--;
                }
            }

            // Assign name based on the unmatched items
            result.Name = String.Join(" ", words);

            return result;
        }
    }
}
