using Common.DotNet.Test;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using YoFi.Core.Models;
using YoFi.Core.SampleGen;
using YoFi.Core.SampleData;
using YoFi.SampleGen;
using System.Text.Json.Serialization;

namespace YoFi.Tests.Core.SampleGen;

[TestClass]
public class GeneratorExeTest
{
    [TestMethod]
    public void LoadConfiguration()
    {
        var stream = SampleData.Open("SampleDataConfiguration.json");
        var config = JsonSerializer.Deserialize<SampleDataConfiguration>(stream, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } } );
        Assert.IsNotNull(config);
        Assert.AreEqual(2,config.Definitions.Count());
        Assert.AreEqual(6,config.Projects.Count());
    }
}