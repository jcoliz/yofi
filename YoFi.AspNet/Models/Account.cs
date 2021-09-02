using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.AspNet.Models
{
    /// <summary>
    /// A partition of transactions owned by a specific user
    /// </summary>
    /// <remarks>
    /// This supports the idea of partitioning the transactions for multiple users.
    /// It has not been implemented yet.
    /// </remarks>
    public class Account
    {
        /// <summary>
        /// Object identity in Entity Framework
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Owning user
        /// </summary>
        /// <remarks>
        /// Should map to ApplicationUser.Id. Perhaps would be better to contain ApplicationUser directly?
        /// </remarks>
        public string User { get; set; }

        /// <summary>
        /// Transactions in this partition
        /// </summary>
        public ICollection<Transaction> Transactions { get; set; }
    }
}