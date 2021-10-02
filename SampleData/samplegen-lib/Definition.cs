using System;
using System.Collections.Generic;
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
            _ => throw new NotImplementedException()
        };

        public static int Year { get; set; } = DateTime.Now.Year;

        private static Random random = new Random();

        private IEnumerable<Transaction> GenerateYearly()
        {
            var day = TimeSpan.FromDays(random.Next(0, 364));
            return new List<Transaction>()
            {
                new Transaction() { Amount = YearlyAmount, Category = Category, Payee = Payee, Timestamp = new DateTime(Year,1,1) + day }
            };
        }
    }

    public enum SchemeEnum { Invalid = 0, Monthly, Yearly };
    public enum JitterEnum { Invalid = 0, None, Low, Moderate, High };
}
