using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YoFi.SampleGen
{
    public class Definition
    {
        public string Payee { get; set; }
        public SchemeEnum Scheme { get; set; }
        public JitterEnum DateJitter { get; set; }

        public string Category { get; set; }
        public decimal YearlyAmount { get; set; }
        public JitterEnum AmountJitter { get; set; }

        public string Group { get; set; }

        public IEnumerable<Transaction> GetTransactions(IEnumerable<Definition> insplits = null)
        {
            // Many Per Week overrides the date jitter to high
            if (Scheme == SchemeEnum.ManyPerWeek)
                DateJitter = JitterEnum.High;

            SetDateWindow();

            var splits = insplits ?? new List<Definition> { this };

            return Scheme switch
            {
                SchemeEnum.Invalid => throw new ApplicationException("Invalid scheme"),
                SchemeEnum.Yearly => GenerateTypical(splits),
                SchemeEnum.Monthly => GenerateTypical(splits),
                SchemeEnum.Quarterly => GenerateTypical(splits),
                SchemeEnum.Weekly => GenerateTypical(splits),
                SchemeEnum.SemiMonthly => GenerateSemiMonthly(splits),
                SchemeEnum.ManyPerWeek => GenerateManyPerWeek(splits),
                _ => throw new NotImplementedException()
            };
        }
        
        public static int Year { get; set; } = DateTime.Now.Year;

        private static Random random = new Random();

        public const int HowManyPerWeek = 3;

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

        public static Dictionary<SchemeEnum, int> SchemeNumPeriods = new Dictionary<SchemeEnum, int>()
        {
            { SchemeEnum.ManyPerWeek, 52 * HowManyPerWeek },
            { SchemeEnum.Weekly, 52 },
            { SchemeEnum.SemiMonthly, 24 },
            { SchemeEnum.Monthly, 12 },
            { SchemeEnum.Quarterly, 4 },
            { SchemeEnum.Yearly, 1 },
        };

        private TimeSpan DateWindowStarts;
        private TimeSpan DateWindowLength;

        private Transaction GenerateOneTransaction(int index, IEnumerable<Definition> splits) =>
            new Transaction() 
            { 
                Payee = Payee,
                Splits = splits.Select(s => new CategoryAmount()
                {
                    Category = s.Category,
                    Amount = s.JitterizeAmount(s.YearlyAmount / SchemeNumPeriods[Scheme])
                }).ToList(),
                Timestamp = Scheme switch
                {
                    SchemeEnum.Monthly => new DateTime(Year, index, 1),
                    SchemeEnum.Yearly => new DateTime(Year, 1, 1),
                    SchemeEnum.Quarterly => new DateTime(Year, index * 3 - 2, 1),
                    SchemeEnum.ManyPerWeek => new DateTime(Year, 1, 1) + TimeSpan.FromDays(7 * (index-1)),
                    SchemeEnum.Weekly => new DateTime(Year, 1, 1) + TimeSpan.FromDays(7 * (index-1)),
                    _ => throw new NotImplementedException()
                } + JitterizedDate
            };

        private IEnumerable<Transaction> GenerateTypical(IEnumerable<Definition> splits) =>
            Enumerable.Range(1, SchemeNumPeriods[Scheme]).Select(x => GenerateOneTransaction(x, splits));

        private IEnumerable<Transaction> GenerateManyPerWeek(IEnumerable<Definition> splits) =>
            Enumerable.Range(1, HowManyPerWeek).SelectMany(x => Enumerable.Range(1, 52).Select(w => GenerateOneTransaction(w, splits))).OrderBy(x => x.Timestamp);

        private IEnumerable<Transaction> GenerateSemiMonthly(IEnumerable<Definition> splits)
        {
            // The "Splits" give us category and amount

            if (DateJitter != JitterEnum.None && DateJitter != JitterEnum.Invalid)
                throw new NotImplementedException("SemiMonthly with date jitter is not implemented");

            var days = new int[] { 1, 15 };

            return Enumerable.Range(1, 12).SelectMany
            (
                month => 
                days.Select
                ( 
                    day =>                
                    new Transaction() 
                    { 
                        Payee = Payee, 
                        Timestamp = new DateTime(Year, month, day),
                        Splits = splits.Select(s=>new CategoryAmount() 
                        { 
                            Category = s.Category, 
                            Amount = s.JitterizeAmount(s.YearlyAmount/24) 
                        }).ToList()
                    }
                )
            );
        }

        private decimal JitterizeAmount(decimal amount) =>
            (AmountJitter == JitterEnum.None) ? amount :
                (decimal)((double)amount * (1.0 + 2.0 * (random.NextDouble() - 0.5) * AmountJitterValues[AmountJitter]));

        private void SetDateWindow()
        {
            // No date windows for semimonthly
            if (Scheme == SchemeEnum.SemiMonthly)
                return;

            // Randomly choose a window. The Window must be entirely within the Scheme Timespan, but chosen at random.
            // The size of the window is given by the Date Jitter.

            DateWindowLength = (DateJitter == JitterEnum.None) ? TimeSpan.FromDays(1) : SchemeTimespans[Scheme] * DateJitterValues[DateJitter];
            DateWindowStarts = TimeSpan.FromDays(random.Next(0, SchemeTimespans[Scheme].Days - DateWindowLength.Days));
        }

        private TimeSpan JitterizedDate => DateWindowStarts + ((DateJitter != JitterEnum.None) ? TimeSpan.FromDays(random.Next(0, DateWindowLength.Days)) : TimeSpan.Zero);
    }

    public enum SchemeEnum { Invalid = 0, ManyPerWeek, Weekly, SemiMonthly, Monthly, Quarterly, Yearly };
    public enum JitterEnum { Invalid = 0, None, Low, Moderate, High };
}
