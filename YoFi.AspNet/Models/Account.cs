using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.AspNet.Models
{
    public class Account
    {
        public int ID { get; set; }
        public string User { get; set; }
        public ICollection<Transaction> Transactions { get; set; }
    }
}
