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

        public IEnumerable<Transaction> GetTransactions()
        {
            // Many Per Week overrides the date jitter to high
            if (Scheme == SchemeEnum.ManyPerWeek)
                DateJitter = JitterEnum.High;

            SetDateWindow();

            return Scheme switch
            {
                SchemeEnum.Invalid => throw new ApplicationException("Invalid scheme"),
                SchemeEnum.Yearly => GenerateYearly(),
                SchemeEnum.Monthly => GenerateMonthly(),
                SchemeEnum.SemiMonthly => GenerateSemiMonthly(),
                SchemeEnum.Quarterly => GenerateQuarterly(),
                SchemeEnum.Weekly => GenerateWeekly(),
                SchemeEnum.ManyPerWeek => GenerateManyPerWeek(),
                _ => throw new NotImplementedException()
            };

        }


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
            { SchemeEnum.Weekly, TimeSpan.FromDays(7) },
            { SchemeEnum.ManyPerWeek, TimeSpan.FromDays(7) },
            { SchemeEnum.Monthly, TimeSpan.FromDays(28) },
            { SchemeEnum.Quarterly, TimeSpan.FromDays(90) },
            { SchemeEnum.Yearly, TimeSpan.FromDays(365) },
        };

        private TimeSpan DateWindowStarts;
        private TimeSpan DateWindowLength;

        private Transaction GenerateOneTransaction(int index, int numperiods)
        {
            var result = new Transaction() { Amount = JitterizeAmount(YearlyAmount / numperiods), Category = Category, Payee = Payee };

            result.Timestamp = Scheme switch
            {
                SchemeEnum.Monthly => new DateTime(Year, index, 1) + JitterizedDate,
                SchemeEnum.Yearly => new DateTime(Year, 1, 1) + JitterizedDate,
                SchemeEnum.Quarterly => new DateTime(Year, index * 3 - 2, 1) + JitterizedDate,
                SchemeEnum.ManyPerWeek => new DateTime(Year, 1, 1) + TimeSpan.FromDays(7 * (index-1)) + JitterizedDate,
                SchemeEnum.Weekly => new DateTime(Year, 1, 1) + TimeSpan.FromDays(7 * (index-1)) + JitterizedDate,
                _ => throw new NotImplementedException()
            };

            return result;
        }

        private IEnumerable<Transaction> GenerateYearly() => Enumerable.Range(1, 1).Select(x => GenerateOneTransaction(x, 1));

        private IEnumerable<Transaction> GenerateMonthly() => Enumerable.Range(1, 12).Select(x => GenerateOneTransaction(x, 12));

        private IEnumerable<Transaction> GenerateQuarterly() => Enumerable.Range(1, 4).Select(x => GenerateOneTransaction(x, 4));

        private IEnumerable<Transaction> GenerateWeekly() => Enumerable.Range(1, 52).Select(x => GenerateOneTransaction(x, 52));

        private IEnumerable<Transaction> GenerateSemiMonthly()
        {
            if (DateJitter != JitterEnum.None && DateJitter != JitterEnum.Invalid)
                throw new NotImplementedException("SemiMonthly with date jitter is not implemented");

            var days = new int[] { 1, 15 };

            return Enumerable.Range(1, 12).SelectMany
            (
                month => 
                days.Select
                ( 
                    day =>                
                    new Transaction() { Amount = JitterizeAmount(YearlyAmount / 24), Category = Category, Payee = Payee, Timestamp = new DateTime(Year, month, day) }
                )
            );
        }


        private IEnumerable<Transaction> GenerateWeekly(decimal amount)
        {
            if (0 == amount)
                amount = YearlyAmount / 52;

            return Enumerable.Range(0, 52).Select
            (
                week => new Transaction() { Amount = JitterizeAmount(amount), Category = Category, Payee = Payee, Timestamp = new DateTime(Year, 1, 1) + TimeSpan.FromDays(7 * week) + JitterizedDate }
            );
        }

        private IEnumerable<Transaction> GenerateManyPerWeek()
        {
            int numperweek = 3;

            return Enumerable.Repeat(0, numperweek).SelectMany(x => Enumerable.Range(1, 52).Select(x => GenerateOneTransaction(x, 52*numperweek))).OrderBy(x=>x.Timestamp);
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
            // No date windows for semimonthly
            if (Scheme == SchemeEnum.SemiMonthly)
                return;

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

    public enum SchemeEnum { Invalid = 0, ManyPerWeek, Weekly, SemiMonthly, Monthly, Quarterly, Yearly };
    public enum JitterEnum { Invalid = 0, None, Low, Moderate, High };
}
