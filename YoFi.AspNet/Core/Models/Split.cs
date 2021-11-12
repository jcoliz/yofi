using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace YoFi.Core.Models
{
    /// <summary>
    /// Used to split transaction amount across multiple
    /// different categories
    /// </summary>
    public class Split: IModelItem<Split>, IReportable
    {
        /// <summary>
        /// Object identity in Entity Framework
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// How much of the transaction should be assigned to this Category
        /// </summary>
        [DisplayFormat(DataFormatString = "{0:C2}")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        /// <summary>
        /// Category for this amount
        /// </summary>        
        public string Category { get; set; }

        /// <summary>
        /// SubCategory for this amount
        /// </summary>        
        /// <remarks>
        /// This property is obsolete. Category can now take multiple
        /// levels of information separated with colons.
        /// </remarks>
        public string SubCategory { get; set; }

        /// <summary>
        /// Optional commentary about this split
        /// </summary>
        public string Memo { get; set; }

        /// <summary>
        /// Reference to the transaction which contains us
        /// </summary>
        public int TransactionID { get; set; }

        /// <summary>
        /// Navigation property for the transaction which contains us
        /// </summary>
        [JsonIgnore]
        public Transaction Transaction { get; set; }

        /// <summary>
        /// Fulfil the reports' need to directly get our timestamp. Be sure
        /// to include the transaction property if sending in for reports!
        /// </summary>
        DateTime IReportable.Timestamp => Transaction?.Timestamp ?? DateTime.MinValue;

        public override bool Equals(object obj)
        {
            return obj is Split split &&
                   Amount == split.Amount &&
                   Category == split.Category &&
                   Memo == split.Memo;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Amount, Category, Memo);
        }

        IQueryable<Split> IModelItem<Split>.InDefaultOrder(IQueryable<Split> original)
        {
            throw new NotImplementedException();
        }
    }
}
