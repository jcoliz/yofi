using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DotNet
{
    public static class Numbers
    {
        public static string ToWords(int amount)
        {
            var result = new List<string>();
            var words = new[] { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            var tens = new[] { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            if (amount < 0)
                amount = -amount;

            if ( amount >= 100000m )
                throw new ArgumentException("Value out of range", nameof(amount));

            if ( amount >= 1000 )
            {
                result.Add(ToWords(amount / 1000) + " Thousand");
                amount = amount % 1000;
            }

            if (amount >= 100)
            {
                result.Add(ToWords(amount / 100) + " Hundred");
                amount = amount % 100;
            }

            if (amount <= 19)
            {
                result.Add(words[amount]);
            }
            else
            {
                var term = tens[amount / 10];
                amount = amount % 10;

                if (amount > 0)
                    term += "-" + ToWords(amount);

                result.Add(term);
            }

            return String.Join(" ", result).Trim();
        }

        public static string ToWords(decimal amount)
        {
            if (amount < 0)
                amount = -amount;

            int dollars = (int)Math.Truncate(amount);

            var units = (dollars > 0) ? ToWords(dollars) : "Zero";

            var pennies = (amount * 100m) % 100m;

            if (pennies > 0)
                return units + $" & {pennies:00}/100";
            else
                return units + " Only";
        }
    }
}
