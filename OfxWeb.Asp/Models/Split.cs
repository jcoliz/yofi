﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Models
{
    public class Split: IID
    {
        public int ID { get; set; }
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Amount { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Memo { get; set; }
        public int TransactionID { get; set; }
        public Transaction Transaction { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Split split &&
                   Amount == split.Amount &&
                   Category == split.Category &&
                   SubCategory == split.SubCategory;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Amount, Category, SubCategory);
        }
    }
}
