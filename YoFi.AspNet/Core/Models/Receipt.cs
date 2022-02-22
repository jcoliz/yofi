using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

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
            var given = Path.GetFileNameWithoutExtension(filename);
            return new Receipt() { Name = given, Filename = filename };
        }
    }
}
