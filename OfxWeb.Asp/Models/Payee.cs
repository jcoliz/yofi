using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Models
{
    public class Payee
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public bool? Selected { get; set; }

        /// <summary>
        /// Remove all characters from payee which are not whitespace or alpha-numeric
        /// </summary>
        public void FixupName()
        {
            Regex rx = new Regex(@"[^\s\w\d]+");
            Name = rx.Replace(Name, new MatchEvaluator(x => string.Empty));
        }
    }
}
