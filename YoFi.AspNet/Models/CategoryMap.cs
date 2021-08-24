using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YoFi.AspNet.Models
{
    /// <summary>
    /// A mapping from one categorization scheme to another
    /// </summary>
    /// <remarks>
    /// At some point I realized I wanted to completely re-engineer how I
    /// represent my categories, so the category mapper was born.
    /// This class holds a single category mapping rule
    /// </remarks>
    public class CategoryMap: IID
    {
        /// <summary>
        /// Object identity in Entity Framework
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Categorization of the existing item to match
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Second-level categorization of the existing item to match
        /// </summary>
        public string SubCategory { get; set; }

        /// <summary>
        /// First-level resulting category
        /// </summary>
        /// <remarks>
        /// After an object has been remapped, the category will consist
        /// of all the Key1-4 items from here separated by colons.
        /// </remarks>
        public string Key1 { get; set; }

        /// <summary>
        /// Second-level resulting category
        /// </summary>
        public string Key2 { get; set; }

        /// <summary>
        /// Third-level resulting category
        /// </summary>
        public string Key3 { get; set; }

        /// <summary>
        /// Fourth-level resulting category
        /// </summary>
        /// <remarks>
        /// As I am phasing out CategoryMap, Key4 is only supported for automatically-generated
        /// mapping rules
        /// </remarks>
        [NotMapped]
        public string Key4 { get; private set; }


        /// <summary>
        /// Whether there is an automatic mapping rule for this <paramref name="category"/>
        /// </summary>
        /// <remarks>
        /// See Product Backlog Item #801: Add an automatic mapping rule for categories with a colon
        /// If Category contains a colon, then we don't need a hard-coded mapping rule for it, 
        /// we can figure it out by definition.
        /// </remarks>
        /// <param name="category"></param>
        /// <returns>True if there is</returns>
        public static bool HasDefaultMapFor(string category) => category?.Contains(':') ?? false;

        /// <summary>
        /// Create the default mapping rule for this category
        /// </summary>
        /// <remarks>
        /// If a category has a colon in it, then there exists a default rule by definition.
        /// This method creates it and returns in.
        /// The rule is: Split category into Key1:Key2, and then look in subcategory for
        /// Key3:Key4.
        /// </remarks>
        /// <param name="category"></param>
        /// <returns></returns>

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

                // User Story 874: Automatically split key3 out of category if enough separators
                if (split.Count() >= 3)
                    result.Key3 = split[2];
                if (split.Count() >= 4)
                    result.Key4 = split[3];
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
                   Key3 == map.Key3 &&
                   Key4 == map.Key4; ;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Category, SubCategory, Key1, Key2, Key3, Key4);
        }
    }

    public class CategoryMapper
    {
        Dictionary<string, CategoryMap> maptable;

        public CategoryMapper(IQueryable<CategoryMap> maps)
        {
            maptable = maps.ToDictionary(x => x.Category + (string.IsNullOrEmpty(x.SubCategory) ? string.Empty : "&" + x.SubCategory), x => x);
        }

        public void MapObject(ICatSubcat item)
        {
            var keys = KeysFor(item.Category, item.SubCategory);
            var joined = string.Join(':', keys).AsEnumerable();
            while (joined.LastOrDefault() == ':')
                joined = joined.SkipLast(1);
            item.Category = new string(joined.ToArray());
            item.SubCategory = null;
        }

        public string[] KeysFor(string Category, string SubCategory)
        {
            var result = new string[] { null, null, null, null };

            if (null == Category)
                return result;

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
                    var re = new Regex("^(.*?)\\.");
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

                if (!string.IsNullOrEmpty(map.Key4))
                {
                    if ("-" == map.Key4 || skipkey3)
                    {
                        // Key4 remains blank
                    }
                    else if (map.Key4.StartsWith('^'))
                    {
                        if (!string.IsNullOrEmpty(SubCategory))
                        {
                            var re = new Regex(map.Key4);
                            var match = re.Match(SubCategory);
                            if (match.Success && match.Groups.Count > 1)
                                result[3] = match.Groups[1].Value;
                        }
                    }
                    else
                        result[3] = map.Key4;
                }
            }

            return result;
        }
    }
}
