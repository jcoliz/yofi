using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.ChartJS
{
    /// <summary>
    /// The chart configuration which can be supplied to a Chart.JS chart
    /// </summary>
    /// <remarks>
    /// The idea is to build this in C# then serialize it directly to Json, and feed the result
    /// to "new Chart()" in javascript
    /// </remarks>
    /// <see href="https://www.chartjs.org/docs/latest/general/data-structures.html"/>
    public class ChartConfig
    {
        public string Type { get; set; }

        public ChartData Data { get; } = new ChartData();

        public ChartOptions Options { get; } = new ChartOptions();

        public static ChartConfig CreatePieChart(IEnumerable<(string Label,int Data)> points, IEnumerable<ChartColor> colors)
        {
            var result = new ChartConfig() { Type = "doughnut" };

            result.FillSingle(points, colors);

            return result;
        }

        public static ChartConfig CreateBarChart(IEnumerable<(string Label, int Data)> points, IEnumerable<ChartColor> colors)
        {
            var result = new ChartConfig() { Type = "bar" };

            result.Options.Plugins.Legend.Display = false;
            result.FillSingle(points, colors);

            return result;
        }

        public static ChartConfig CreateLineChart(IEnumerable<string> labels, IEnumerable<(string Label,IEnumerable<int> Data)> series, IEnumerable<ChartColor> colors)
        {
            var result = new ChartConfig() { Type = "line" };

            const int maxitems = 6;

            // Limit to only {maxitems} series
            var numitems = series.Count();
            if (numitems > maxitems)
            {
                series = series.Take(maxitems);
            }

            result.Data.Labels = labels;
            result.Data.Datasets = series.Select((x, i) => new ChartDataSet() { Label = x.Label, Data = x.Data, BackgroundColor = new ChartColor[] { colors.Skip(i).First() }, BorderColor = new ChartColor[] { colors.Skip(i).First() } });

            return result;
        }
        private void FillSingle(IEnumerable<(string Label, int Data)> points, IEnumerable<ChartColor> colors)
        {
            const int maxpoints = 7;

            // Reduce to maxitems. Put the rest under "others"
            var numitems = points.Count();
            if (numitems > maxpoints)
            {
                numitems = maxpoints;
                var total = points.Skip(maxpoints - 1).Sum(x => x.Data);
                points = points.Take(maxpoints - 1).Append(("Others", total));
            }

            // Set labels
            Data.Labels = points.Select(x => x.Label);

            // Set data values
            Data.Datasets = new List<ChartDataSet>() { new ChartDataSet() { Data = points.Select(x => x.Data), BorderWidth = 2 } };

            // Set colors            
            Data.Datasets.Last().BorderColor = colors.Take(numitems);
            Data.Datasets.Last().BackgroundColor = colors.Take(numitems).Select(x => x.WithAlpha(0.8));
        }

    };

    public class ChartDataSet
    {
        public string Label { get; set; } = null;
        public IEnumerable<int> Data { get; set; }
        public IEnumerable<ChartColor> BackgroundColor { get; set; }
        public IEnumerable<ChartColor> BorderColor { get; set; }
        public int? BorderWidth { get; set; }
    }

    public class ChartData
    {
        public IEnumerable<string> Labels { get; set; }

        public IEnumerable<ChartDataSet> Datasets { get; set; }
    }

    public class ChartLegend
    {
        public string Position { get; set; } = "bottom";
        public bool Display { get; set; } = true;
    }

    public class ChartPlugins
    {
        public ChartLegend Legend { get; set; } = new ChartLegend();
    }

    public class ChartOptions
    {
        public ChartPlugins Plugins { get; set; } = new ChartPlugins();
    }

}