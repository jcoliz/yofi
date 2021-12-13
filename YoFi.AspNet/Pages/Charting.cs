using System;

namespace YoFi.AspNet.Pages.Charting
{
    public class ChartDataSet
    {
        public int[] Data {get;set;}
        public string[] BackgroundColor {get;set;}
        public string[] BorderColor {get;set;}
        public int BorderWidth {get;set;} = 1;
    }

    public class ChartData
    {
        public string[] Labels { get; set; }

        public ChartDataSet[] Datasets { get; set; } = new ChartDataSet[] { new ChartDataSet() };
    }

    public class ChartLegend
    {
        public string Position { get; set; } = "bottom";
    }

    public class ChartPlugins
    {
        public ChartLegend Legend { get; set; } = new ChartLegend();
    }
    public class ChartOptions
    {
        public ChartPlugins Plugins { get; set; } = new ChartPlugins();
    }

    public class ChartDef
    {
        public string Type { get; set; }

        public ChartData Data { get; set; } = new ChartData();

        public ChartOptions Options { get; set; } = new ChartOptions();
    };

}