using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Models
{
    public interface IID
    {
        int ID { get; set;  }
    }

    public class CategoryMap: IID
    {
        public int ID { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Key1 { get; set; }
        public string Key2 { get; set; }
        public string Key3 { get; set; }

        // As I am phasing out CategoryMap, Key4 is only supported for automatically-generated
        // mapping rules
        [NotMapped]
        public string Key4 { get; private set; }

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
                result.Key4 = "^[^\\:]*\\:([^\\:]+)";
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

    public class CategoryMapper
    {
        Dictionary<string, CategoryMap> maptable;

        public CategoryMapper(IQueryable<CategoryMap> maps)
        {
            maptable = maps.ToDictionary(x => x.Category + (string.IsNullOrEmpty(x.SubCategory) ? string.Empty : "&" + x.SubCategory), x => x);
        }

        public void MapTransaction(Models.Transaction transaction)
        {
            var keys = KeysFor(transaction.Category, transaction.SubCategory);
            var joined = string.Join(':', keys).AsEnumerable();
            while (joined.LastOrDefault() == ':')
                joined = joined.SkipLast(1);
            transaction.Category = new string(joined.ToArray());
            transaction.SubCategory = null;
        }

        public string[] KeysFor(string Category, string SubCategory)
        {
            var result = new string[] { null, null, null, null };

            CategoryMap map = null;
            if (CategoryMap.HasDefaultMapFor(Category))
                map = CategoryMap.DefaultFor(Category);
            if (maptable.ContainsKey(Category))
                map = maptable[Category];
            if (!string.IsNullOrEmpty(SubCategory))
            {
                var key = Category + "&" + SubCategory;
                if (maptable.ContainsKey(key))
                    map = maptable[key];
                else
                {
                    var re = new Regex("^([^\\.]*)\\.");
                    var match = re.Match(SubCategory);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        key = Category + "&^" + match.Groups[1].Value;
                        if (maptable.ContainsKey(key))
                            map = maptable[key];
                    }
                }
            }
            if (null == map)
            {
                result[0] = "Unmapped";
                result[1] = Category;
                if (!string.IsNullOrEmpty(SubCategory))
                    result[2] = SubCategory;
            }
            else
            {
                result[0] = map.Key1;

                bool skipkey3 = false;
                if (string.IsNullOrEmpty(map.Key2))
                {
                    if (string.IsNullOrEmpty(SubCategory))
                    {
                        result[1] = Category;
                    }
                    else
                    {
                        result[1] = SubCategory;
                    }
                    skipkey3 = true;
                }
                else if (map.Key2.StartsWith('^'))
                {
                    if (!string.IsNullOrEmpty(SubCategory))
                    {
                        var re = new Regex(map.Key2);
                        var match = re.Match(SubCategory);
                        if (match.Success && match.Groups.Count > 1)
                            result[1] = match.Groups[1].Value;
                    }
                }
                else
                    result[1] = map.Key2;

                if ("-" == map.Key3 || skipkey3)
                {
                    // Key3 remains blank
                }
                else if (string.IsNullOrEmpty(map.Key3))
                {
                    result[2] = SubCategory;
                }
                else if (map.Key3.StartsWith('^'))
                {
                    if (!string.IsNullOrEmpty(SubCategory))
                    {
                        var re = new Regex(map.Key3);
                        var match = re.Match(SubCategory);
                        if (match.Success && match.Groups.Count > 1)
                            result[2] = match.Groups[1].Value;
                    }
                }
                else
                    result[2] = map.Key3;

                if (!string.IsNullOrEmpty(map.Key4) && !string.IsNullOrEmpty(SubCategory))
                {
                    var re = new Regex(map.Key4);
                    var match = re.Match(SubCategory);
                    if (match.Success && match.Groups.Count > 1)
                        result[3] = match.Groups[1].Value;
                }
            }

            return result;
        }
    }
}
