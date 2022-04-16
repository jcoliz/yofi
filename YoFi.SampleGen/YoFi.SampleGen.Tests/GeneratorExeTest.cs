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
        var runner = JsonSerializer.Deserialize<SampleDataRunner>(stream, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } } );
        Assert.IsNotNull(runner);
        Assert.AreEqual(2,runner.Definitions.Count());
        Assert.AreEqual(6,runner.Projects.Count());
    }
}