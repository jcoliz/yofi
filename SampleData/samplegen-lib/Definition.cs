using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YoFi.SampleGen
{
    public class Definition
    {
        public string Category { get; set; }
        public string Payee { get; set; }
        public decimal YearlyAmount { get; set; }
        public SchemeEnum Scheme { get; set; }
        public JitterEnum DateJitter { get; set; }
        public JitterEnum AmountJitter { get; set; }

        public IEnumerable<Transaction> GetTransactions() => Scheme switch
        {
            SchemeEnum.Invalid => throw new ApplicationException("Invalid scheme"),
            SchemeEnum.Yearly => GenerateYearly(),
            SchemeEnum.Monthly => GenerateMonthly(),
            _ => throw new NotImplementedException()
        };

        public static int Year { get; set; } = DateTime.Now.Year;

        private static Random random = new Random();

        public static Dictionary<JitterEnum, decimal> AmountJitterValues = new Dictionary<JitterEnum, decimal>()
        {
            { JitterEnum.None, 0 },
            { JitterEnum.Low, 0.1m },
            { JitterEnum.Moderate, 0.4m },
            { JitterEnum.High, 0.9m }
        };

        private IEnumerable<Transaction> GenerateYearly()
        {
            var day = TimeSpan.FromDays(random.Next(0, 364));

            return new List<Transaction>()
            {
                new Transaction() { Amount = Jitterize(YearlyAmount), Category = Category, Payee = Payee, Timestamp = new DateTime(Year,1,1) + day }
            };
        }

        private IEnumerable<Transaction> GenerateMonthly()
        {
            var day = TimeSpan.FromDays(random.Next(0, 28));

            return Enumerable.Range(1, 12).Select
            (
                month => new Transaction() { Amount = Jitterize(YearlyAmount/12), Category = Category, Payee = Payee, Timestamp = new DateTime(Year, month, 1) + day }
            );
        }

        private decimal Jitterize(decimal amount)
        {
            if (AmountJitter != JitterEnum.None)
            {
                var amountjittervalue = AmountJitterValues[AmountJitter];
                amount = (decimal)((double)amount * (1.0 + 2.0 * (random.NextDouble() - 0.5) * (double)amountjittervalue));
            }

            return amount;
        }
    }

    public enum SchemeEnum { Invalid = 0, Monthly, Yearly };
    public enum JitterEnum { Invalid = 0, None, Low, Moderate, High };
}
