using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core.Models
{
    /// <summary>
    /// Budget Transaction (Budget line item)
    /// </summary>
    /// <remarks>
    /// Represents a single expected outlay of money into a specific account
    /// in a specific timeframe.
    /// </remarks>
    public class BudgetTx: IReportable, IModelItem<BudgetTx>
    {
        /// <summary>
        /// Object identity in Entity Framework
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The amount of expected outlay (typicaly, or income if positive)
        /// </summary>
        [DisplayFormat(DataFormatString = "{0:C2}")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Timeframe of expected outlay
        /// </summary>
        /// <remarks>
        /// Current practice is to have a single budget trasnaction in a year for
        /// year-long budget, and then multiple for budget that becomes available
        /// over time.
        /// </remarks>
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Category of expected outlay
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public BudgetTx() { }

        /// <summary>
        /// Copy constructor, but use a new <paramref name="date"/>
        /// </summary>
        /// <param name="source">Item to copy from</param>
        /// <param name="date">New date to use instead of the one in <paramref name="source"/></param>
        public BudgetTx(BudgetTx source,DateTime date)
        {
            Amount = source.Amount;
            Category = source.Category;
            Timestamp = date;
        }

        // TODO: This can be combined with ImportEquals. ImportEquals is actually a better equality comparer

        public override bool Equals(object obj)
        {
            return obj is BudgetTx tx &&
                   Timestamp == tx.Timestamp &&
                   Category == tx.Category;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Timestamp, Category);
        }

        public IQueryable<BudgetTx> InDefaultOrder(IQueryable<BudgetTx> original)
        {
            return original.OrderByDescending(x => x.Timestamp.Year).ThenByDescending(x => x.Timestamp.Month).ThenBy(x => x.Category);
        }

        int IModelItem<BudgetTx>.GetImportHashCode() =>
            HashCode.Combine(Timestamp.Year, Timestamp.Month, Category);

        bool IModelItem<BudgetTx>.ImportEquals(BudgetTx other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            return Timestamp.Year == other.Timestamp.Year && Timestamp.Month == other.Timestamp.Month && Category == other.Category;
        }
    }
}
