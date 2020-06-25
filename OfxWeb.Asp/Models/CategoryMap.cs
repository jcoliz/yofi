using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Models
{
    public class CategoryMap
    {
        public int ID { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Key1 { get; set; }
        public string Key2 { get; set; }
        public string Key3 { get; set; }

        /// <summary>
        /// Remove all characters from all fields which are not whitespace or alpha-numeric
        /// </summary>
        public void Fixup()
        {
            Regex rx = new Regex(@"[^\s\w\d]+");
            if ( !string.IsNullOrEmpty(Category))
                Category = rx.Replace(Category, new MatchEvaluator(x => string.Empty));
            if (!string.IsNullOrEmpty(SubCategory))
                SubCategory = rx.Replace(SubCategory, new MatchEvaluator(x => string.Empty));
            if (!string.IsNullOrEmpty(Key1))
                Key1 = rx.Replace(Key1, new MatchEvaluator(x => string.Empty));
            if (!string.IsNullOrEmpty(Key2))
                Key2 = rx.Replace(Key2, new MatchEvaluator(x => string.Empty));
            if (!string.IsNullOrEmpty(Key3))
                Key3 = rx.Replace(Key3, new MatchEvaluator(x => string.Empty));
        }
    }
}
