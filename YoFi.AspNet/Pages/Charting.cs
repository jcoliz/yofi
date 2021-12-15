using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        public ChartColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                throw new ArgumentNullException(nameof(hex));

            if (hex[0] == '#')
                hex = hex.Substring(1);

            if (hex.Length != 6 && hex.Length != 8)
                throw new ArgumentOutOfRangeException(nameof(hex), "Expected 6 or 8 hex characters");

            byte parse(string param, int from)
            {
                byte result = default;

                if (!byte.TryParse(hex.Substring(from, 2), NumberStyles.HexNumber, null, out result))
                    throw new ArgumentOutOfRangeException(param, "Can't convert from hex");

                return result;
            }

            Red = parse(nameof(Red), 0);
            Green = parse(nameof(Green), 2);
            Blue = parse(nameof(Blue), 4);

            if (hex.Length == 8)
            {
                Alpha = parse(nameof(Alpha), 6);
            }
            else
                Alpha = byte.MaxValue;
        }

        public ChartColor WithAlpha(double alpha)
        {
            return new ChartColor(Red, Green, Blue, alpha);
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
        public IEnumerable<int> Data { get; set; }
        public IEnumerable<ChartColor> BackgroundColor { get; set; }
        public IEnumerable<ChartColor> BorderColor { get; set; }
        public int BorderWidth { get; set; } = 1;
    }

    public class ChartData
    {
        public IEnumerable<string> Labels { get; set; }

        public IEnumerable<ChartDataSet> Datasets { get; set; }
    }

    public class ChartDataPoint
    {
        public string Label { get; set; }
        public int Data { get; set; }
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

        public ChartData Data { get; } = new ChartData();

        public ChartOptions Options { get; } = new ChartOptions();

        public void SetDataPoints(IEnumerable<ChartDataPoint> points)
        {
            const int maxpoints = 6;

            // Reduce to maxitems. Put the rest under "others"
            var numitems = points.Count();
            if (numitems > maxpoints)
            {
                var total = points.Skip(maxpoints-1).Sum(x => x.Data);
                points = points.Take(maxpoints-1).Append(new ChartDataPoint() { Label = "Others", Data = total });
            }

            // Set labels
            Data.Labels = points.Select(x => x.Label);

            // Set data values
            Data.Datasets = new List<ChartDataSet>() { new ChartDataSet() { Data = points.Select(x => x.Data) } };

            // Set colors            
            Data.Datasets.Last().BorderColor = palette.Take(numitems);
            Data.Datasets.Last().BackgroundColor = palette.Take(numitems).Select(x => x.WithAlpha(0.5));
        }

        private static ChartColor[] palette = new ChartColor[]
        {
            new ChartColor("540D6E"),
            new ChartColor("EE4266"),
            new ChartColor("FFD23F"),
            new ChartColor("313628"),
            new ChartColor("3A6EA5"),
            new ChartColor("7A918D"),
            new ChartColor("7F7C4A"),
        };
    };

}