using System.Text.Json;
using System.Text.Json.Serialization;
using YoFi.SampleGen;

Console.WriteLine("YoFi.SampleGen.Exe: Generate sample data for project");

SampleDataPattern.Year = DateTime.Now.Year;
var stream = File.Open("SampleDataConfiguration.json",FileMode.Open);
var config = JsonSerializer.Deserialize<SampleDataConfiguration>(stream, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } } );
var runner = new SampleDataRunner();

runner.Load(config);

foreach(var project in config.Projects)
{
    Console.WriteLine($"> {project.Name}");
    var files = runner.Run(project);
    foreach(var file in files)
        Console.WriteLine($"\t{file}");
}
