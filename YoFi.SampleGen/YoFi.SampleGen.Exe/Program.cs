using System.Text.Json;
using System.Text.Json.Serialization;
using YoFi.SampleGen;
using YoFi.SampleGen.Exe;

Console.WriteLine("YoFi.SampleGen.Exe: Generate sample data for project");

var options = new AppOptions();
options.Parse(args);
if (options.Help)
{
    options.WriteOptionDescriptions(Console.Out);
    return;
}
else
    Console.WriteLine("\tRun with --help for details");

SampleDataPattern.Year = options.Year ?? DateTime.Now.Year;

var stream = File.Open("SampleDataConfiguration.json", FileMode.Open);
var runner = JsonSerializer.Deserialize<SampleDataRunner>(stream, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } });
runner.Load();

var projects =
    options.Projects.Any() ?
    options.Projects.SelectMany(x => runner.Projects.Where(p => p.Name == x)) :
    runner.Projects;

foreach (var project in projects)
{
    Console.WriteLine($"> {project.Name}");

    var directory = $"out/{project.Name}";
    var files = runner.Run(project,directory);

    foreach (var file in files)
        Console.WriteLine($"\t{file}");
}
