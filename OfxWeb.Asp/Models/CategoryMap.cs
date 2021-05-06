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

        // See Product Backlog Item #801: Add an automatic mapping rule for categories with a colon
        // If Category contains a colon, then we don't need a hard-coded mapping rule for it, 
        // we can figure it out by definition.

        public static bool HasDefaultMapFor(string category) => category.Contains(':');
        public static CategoryMap DefaultFor(string category)
        {
            CategoryMap result = null;

            if (HasDefaultMapFor(category))
            {
                result = new CategoryMap() { Category = category };

                var split = category.Split(':');
                result.Key1 = split[0];
                result.Key2 = split[1];
                result.Key3 = "^([^\\:]*)";
                //result.Key4 = "^[^\\.]*\\:(.+)";
            }

            return result;
        }

        public override bool Equals(object obj)
        {
            return obj is CategoryMap map &&
                   Category == map.Category &&
                   SubCategory == map.SubCategory &&
                   Key1 == map.Key1 &&
                   Key2 == map.Key2 &&
                   Key3 == map.Key3;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Category, SubCategory, Key1, Key2, Key3);
        }
    }
}
