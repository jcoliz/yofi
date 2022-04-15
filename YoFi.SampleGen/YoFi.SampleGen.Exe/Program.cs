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
    Console.WriteLine($"\t{project.Name}");
    var directory = runner.Run(project);
    Console.WriteLine($"\t\tCreated {project.Outputs.Count()} files in {directory}");
}
