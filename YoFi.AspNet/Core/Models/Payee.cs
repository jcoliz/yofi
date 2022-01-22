using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YoFi.Core.Models
{
    /// <summary>
    /// Payee matching rule
    /// </summary>
    /// <remarks>
    /// If a transaction's matches our Name, then it should have this
    /// Category
    /// </remarks>
    public class Payee: IModelItem<Payee>, IImportDuplicateComparable
    {
        /// <summary>
        /// Object identity in Entity Framework
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Name of payee, or regex rule
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Category to assign to matching transactions
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Whether this object will be included in the next bulk operation
        /// </summary>
        public bool? Selected { get; set; }

        /// <summary>
        /// Remove all characters from payee which are not whitespace or alpha-numeric
        /// </summary>
        public void RemoveWhitespaceFromName()
        {
            Regex rx = new Regex(@"[^\s\w\d]+");
            Name = rx.Replace(Name, new MatchEvaluator(x => string.Empty));
        }

        public override bool Equals(object obj)
        {
            return obj is Payee payee &&
                   Name == payee.Name &&
                   Category == payee.Category;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Category);
        }

        public IQueryable<Payee> InDefaultOrder(IQueryable<Payee> original)
        {
            return original.OrderBy(x => x.Category).ThenBy(x=>x.Name);
        }

        int IImportDuplicateComparable.GetImportHashCode() => Name?.GetHashCode() ?? 0;

        bool IImportDuplicateComparable.ImportEquals(object other) => other is Payee && Name == (other as Payee).Name;
    }
}
