using Common.DotNet.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YoFi.SampleGen.Tests.Unit;

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