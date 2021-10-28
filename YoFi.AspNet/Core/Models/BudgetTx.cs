﻿using System;
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
    public class BudgetTx: IReportable, IModelItem
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

        IEqualityComparer<object> IModelItem.ImportDuplicateComparer => new __BudgetTxImportDuplicateComparer();

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
    }

    /// <summary>
    /// Tells us whether two items are duplicates for the purposes of importing
    /// </summary>
    /// <remarks>
    /// Generally, we don't import duplicates, although some importers override this behavior
    /// </remarks>
    class __BudgetTxImportDuplicateComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y)
        {
            if (x == null || y == null)
                throw new ArgumentNullException("Only works with BudgetTx items");

            if (!(x is BudgetTx) || !(y is BudgetTx))
                throw new ArgumentException("Only works with BudgetTx items");

            var itemx = x as BudgetTx;
            var itemy = y as BudgetTx;

            return itemx.Timestamp.Year == itemy.Timestamp.Year && itemx.Timestamp.Month == itemy.Timestamp.Month && itemx.Category == itemy.Category;
        }
        public int GetHashCode(object obj)
        {
            if (!(obj is BudgetTx))
                throw new ArgumentException("Only works with BudgetTx items");

            var item = obj as BudgetTx;

            return HashCode.Combine(item.Timestamp.Year,item.Timestamp.Month,item.Category);
        }
    }
}