using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YoFi.AspNet.Pages.Charting
{
    [JsonConverter(typeof(ChartColorJsonConverter))]
    public class ChartColor
    {
        private readonly byte Red, Green, Blue, Alpha;

        public ChartColor(int red, int green, int blue, double alpha)
        {
            if (red < 0 || red > 255)
                throw new ArgumentOutOfRangeException(nameof(red), "Allowed range 0-255");
            if (green < 0 || green > 255)
                throw new ArgumentOutOfRangeException(nameof(green), "Allowed range 0-255");
            if (blue < 0 || blue > 255)
                throw new ArgumentOutOfRangeException(nameof(blue), "Allowed range 0-255");
            if (alpha < 0 || alpha > 1.0)
                throw new ArgumentOutOfRangeException(nameof(alpha), "Allowed range 0.0-1.0");

            Red = (byte)red;
            Green = (byte)green;
            Blue = (byte)blue;
            Alpha = (byte)(alpha * 255.0);
        }

        public override string ToString()
        {
            return $"#{Red:X2}{Green:X2}{Blue:X2}{Alpha:X2}";
        }
    }

    public class ChartColorJsonConverter : JsonConverter<ChartColor>
    {
        public override ChartColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, ChartColor value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class ChartDataSet
    {
        public int[] Data {get;set;}
        public ChartColor[] BackgroundColor {get;set;}
        public ChartColor[] BorderColor {get;set;}
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