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
            SchemeEnum.Quarterly => GenerateQuarterly(),
            _ => throw new NotImplementedException()
        };

        public static int Year { get; set; } = DateTime.Now.Year;

        private static Random random = new Random();

        public static Dictionary<JitterEnum, double> AmountJitterValues = new Dictionary<JitterEnum, double>()
        {
            { JitterEnum.None, 0 },
            { JitterEnum.Low, 0.1 },
            { JitterEnum.Moderate, 0.4 },
            { JitterEnum.High, 0.9 }
        };

        public static Dictionary<JitterEnum, double> DateJitterValues = new Dictionary<JitterEnum, double>()
        {
            { JitterEnum.None, 0 },
            { JitterEnum.Low, 0.25 },
            { JitterEnum.Moderate, 0.4 },
            { JitterEnum.High, 1.0 }
        };

        public static Dictionary<SchemeEnum, TimeSpan> SchemeTimespans = new Dictionary<SchemeEnum, TimeSpan>()
        {
            { SchemeEnum.Monthly, TimeSpan.FromDays(28) },
            { SchemeEnum.Quarterly, TimeSpan.FromDays(90) },
            { SchemeEnum.Yearly, TimeSpan.FromDays(365) },
        };

        private TimeSpan DateWindowStarts;
        private TimeSpan DateWindowLength;

        private IEnumerable<Transaction> GenerateYearly()
        {
            var day = TimeSpan.FromDays(random.Next(0, 364));

            return new List<Transaction>()
            {
                new Transaction() { Amount = JitterizeAmount(YearlyAmount), Category = Category, Payee = Payee, Timestamp = new DateTime(Year,1,1) + day }
            };
        }

        private IEnumerable<Transaction> GenerateMonthly()
        {
            SetDateWindow();

            return Enumerable.Range(1, 12).Select
            (
                month => new Transaction() { Amount = JitterizeAmount(YearlyAmount/12), Category = Category, Payee = Payee, Timestamp = new DateTime(Year, month, 1) + JitterizedDate }
            );
        }

        private IEnumerable<Transaction> GenerateQuarterly()
        {
            SetDateWindow();

            return Enumerable.Range(1, 4).Select
            (
                q => new Transaction() { Amount = JitterizeAmount(YearlyAmount / 4), Category = Category, Payee = Payee, Timestamp = new DateTime(Year, q*3-2, 1) + JitterizedDate }
            );
        }

        private decimal JitterizeAmount(decimal amount)
        {
            if (AmountJitter != JitterEnum.None)
            {
                var amountjittervalue = AmountJitterValues[AmountJitter];
                amount = (decimal)((double)amount * (1.0 + 2.0 * (random.NextDouble() - 0.5) * amountjittervalue));
            }

            return amount;
        }

        private void SetDateWindow()
        {
            // Randomly choose a window. The Window must be entirely within the Scheme Timespan, but chosen at random.
            // The size of the window is given by the Date Jitter.

            DateWindowLength = TimeSpan.FromDays(1);
            if (DateJitter != JitterEnum.None)
            {
                DateWindowLength = SchemeTimespans[Scheme] * DateJitterValues[DateJitter];
            }
            DateWindowStarts = TimeSpan.FromDays(random.Next(0, SchemeTimespans[Scheme].Days - DateWindowLength.Days));
        }

        private TimeSpan JitterizedDate => DateWindowStarts + ((DateJitter != JitterEnum.None) ? TimeSpan.FromDays(random.Next(0, DateWindowLength.Days)) : TimeSpan.Zero);

    }

    public enum SchemeEnum { Invalid = 0, Monthly, Quarterly, Yearly };
    public enum JitterEnum { Invalid = 0, None, Low, Moderate, High };
}
