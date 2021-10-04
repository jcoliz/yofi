using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YoFi.AspNet.Models;

namespace YoFi.SampleGen
{
    public class Definition
    {
        public string Payee { get; set; }
        public FrequencyEnum DateFrequency { get; set; }
        public JitterEnum DateJitter { get; set; }

        public string Category { get; set; }
        public decimal AmountYearly { get; set; }
        public JitterEnum AmountJitter { get; set; }

        public string Group { get; set; }

        public static int Year { get; set; } = DateTime.Now.Year;

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

        public static Dictionary<FrequencyEnum, TimeSpan> SchemeTimespans = new Dictionary<FrequencyEnum, TimeSpan>()
        {
            { FrequencyEnum.Weekly, TimeSpan.FromDays(7) },
            { FrequencyEnum.ManyPerWeek, TimeSpan.FromDays(7) },
            { FrequencyEnum.Monthly, TimeSpan.FromDays(28) },
            { FrequencyEnum.Quarterly, TimeSpan.FromDays(90) },
            { FrequencyEnum.Yearly, TimeSpan.FromDays(365) },
        };

        public static Dictionary<FrequencyEnum, int> FrequencyPerYear = new Dictionary<FrequencyEnum, int>()
        {
            { FrequencyEnum.ManyPerWeek, 52 * HowManyPerWeek },
            { FrequencyEnum.Weekly, 52 },
            { FrequencyEnum.SemiMonthly, 24 },
            { FrequencyEnum.Monthly, 12 },
            { FrequencyEnum.Quarterly, 4 },
            { FrequencyEnum.Yearly, 1 },
        };

        private readonly int[] SemiWeeklyDays = new int[] { 1, 15 };

        public IEnumerable<Transaction> GetTransactions(IEnumerable<Definition> insplits = null)
        {
            // Many Per Week overrides the date jitter to high
            if (DateFrequency == FrequencyEnum.ManyPerWeek)
                DateJitter = JitterEnum.High;

            // Check for invalid parameter combinations
            if (DateFrequency == FrequencyEnum.SemiMonthly && DateJitter != JitterEnum.None && DateJitter != JitterEnum.Invalid)
                throw new NotImplementedException("SemiMonthly with date jitter is not implemented");

            // Randomly choose a window. The Window must be entirely within the Scheme Timespan, but chosen at random.
            // The size of the window is given by the Date Jitter.
            if (DateFrequency != FrequencyEnum.SemiMonthly)
            {
                DateWindowLength = (DateJitter == JitterEnum.None) ? TimeSpan.FromDays(1) : SchemeTimespans[DateFrequency] * DateJitterValues[DateJitter];
                DateWindowStarts = TimeSpan.FromDays(random.Next(0, SchemeTimespans[DateFrequency].Days - DateWindowLength.Days));
            }

            Payees = Payee.Split(",").ToList();

            var splits = insplits ?? new List<Definition> { this };

            return DateFrequency switch
            {
                FrequencyEnum.Invalid => throw new ApplicationException("Invalid scheme"),
                FrequencyEnum.Yearly => GenerateTypical(splits),
                FrequencyEnum.Monthly => GenerateTypical(splits),
                FrequencyEnum.Quarterly => GenerateTypical(splits),
                FrequencyEnum.Weekly => GenerateTypical(splits),
                FrequencyEnum.SemiMonthly => GenerateSemiMonthly(splits),
                FrequencyEnum.ManyPerWeek => GenerateManyPerWeek(splits),
                _ => throw new NotImplementedException()
            };
        }

        private static Random random = new Random();
        private TimeSpan DateWindowStarts;
        private TimeSpan DateWindowLength;

        private IEnumerable<Transaction> GenerateTypical(IEnumerable<Definition> splits) =>
            Enumerable.Range(1, FrequencyPerYear[DateFrequency]).Select(x => GenerateTypicalTransaction(x, splits));

        private IEnumerable<Transaction> GenerateManyPerWeek(IEnumerable<Definition> splits) =>
            Enumerable.Range(1, HowManyPerWeek).SelectMany(x => Enumerable.Range(1, 52).Select(w => GenerateTypicalTransaction(w, splits))).OrderBy(x => x.Timestamp);

        private IEnumerable<Transaction> GenerateSemiMonthly(IEnumerable<Definition> splits) =>
            Enumerable.Range(1, 12).SelectMany(month => SemiWeeklyDays.Select(day => GenerateBaseTransaction(splits, new DateTime(Year, month, day))));

        private Transaction GenerateTypicalTransaction(int index, IEnumerable<Definition> splits) =>
            GenerateBaseTransaction(splits,
                DateFrequency switch
                {
                    FrequencyEnum.Monthly => new DateTime(Year, index, 1),
                    FrequencyEnum.Yearly => new DateTime(Year, 1, 1),
                    FrequencyEnum.Quarterly => new DateTime(Year, index * 3 - 2, 1),
                    FrequencyEnum.ManyPerWeek => new DateTime(Year, 1, 1) + TimeSpan.FromDays(7 * (index - 1)),
                    FrequencyEnum.Weekly => new DateTime(Year, 1, 1) + TimeSpan.FromDays(7 * (index - 1)),
                    _ => throw new NotImplementedException()
                } + JitterizedDate
            );

        private Transaction GenerateBaseTransaction(IEnumerable<Definition> splits, DateTime timestamp)
        {
            var generatedsplits = splits.Select(s => new Split()
            {
                Category = s.Category,
                Amount = s.JitterizeAmount(s.AmountYearly / FrequencyPerYear[DateFrequency])
            }).ToList();

            return new Transaction()
            {
                Payee = JitterizedPayee,
                Splits = generatedsplits.Count > 1 ? generatedsplits : null,
                Timestamp = timestamp,
                Category = generatedsplits.Count == 1 ? generatedsplits.Single().Category : null,
                Amount = generatedsplits.Sum(x => x.Amount)
            };
        }

        private List<string> Payees;

        private decimal JitterizeAmount(decimal amount) =>
            (AmountJitter == JitterEnum.None) ? amount :
                (decimal)((double)amount * (1.0 + 2.0 * (random.NextDouble() - 0.5) * AmountJitterValues[AmountJitter]));

        private TimeSpan JitterizedDate => 
            DateWindowStarts + ((DateJitter != JitterEnum.None) ? TimeSpan.FromDays(random.Next(0, DateWindowLength.Days)) : TimeSpan.Zero);

        private string JitterizedPayee => Payees[random.Next(0, Payees.Count)];
    }

    public enum FrequencyEnum { Invalid = 0, ManyPerWeek, Weekly, SemiMonthly, Monthly, Quarterly, Yearly };
    public enum JitterEnum { Invalid = 0, None, Low, Moderate, High };
}
