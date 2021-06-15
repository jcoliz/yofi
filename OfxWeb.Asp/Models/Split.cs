using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Models
{
    public class Split: IID, ISubReportable, ICatSubcat
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

        string IReportable.Category
        {
            get
            {
                if (string.IsNullOrEmpty(SubCategory))
                    return Category;
                else
                    return $"{Category}:{SubCategory}";
            }
        }

        /// <summary>
        /// If category has >2 colon-divided parts, move items 3+ into subcategory
        /// </summary>
        /// <remarks>
        /// This is part of the transition to putting all parts of category into
        /// "category" field and removing subcategory.
        /// </remarks>
        public void FixupCategories()
        {
            // Are there more than one colon in here?
            if (Category.Count(x=>x==':') > 1)
            {
                // If so, break apart category into componenet parts
                var rawparts = Category.Split(':');

                // Empty parts are not allowed
                var parts = rawparts.Where(x => !string.IsNullOrEmpty(x));

                Category = string.Join(':',parts.Take(2));
                if (parts.Count() > 2)
                    SubCategory = string.Join(':', parts.Skip(2));
            }
        }
    }
}
