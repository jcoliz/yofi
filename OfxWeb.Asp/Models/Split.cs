﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Models
{
    public class Split: IID, ISubReportable
    {
        public int ID { get; set; }
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Amount { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Memo { get; set; }
        public int TransactionID { get; set; }
        [JsonIgnore]
        public Transaction Transaction { get; set; }

        DateTime IReportable.Timestamp => Transaction?.Timestamp ?? DateTime.MinValue;
    }
}
