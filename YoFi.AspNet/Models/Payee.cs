using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YoFi.AspNet.Models
{
    public class Payee: IID, ICatSubcat
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public bool? Selected { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Payee payee &&
                   Name == payee.Name &&
                   Category == payee.Category &&
                   SubCategory == payee.SubCategory;
        }

        /// <summary>
        /// Remove all characters from payee which are not whitespace or alpha-numeric
        /// </summary>
        public void FixupName()
        {
            Regex rx = new Regex(@"[^\s\w\d]+");
            Name = rx.Replace(Name, new MatchEvaluator(x => string.Empty));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Category, SubCategory);
        }
    }
}
