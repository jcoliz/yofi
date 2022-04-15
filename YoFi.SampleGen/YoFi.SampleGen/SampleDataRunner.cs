using System.Collections.Generic;
using System.IO;
using System.Linq;
using jcoliz.OfficeOpenXml.Serializer;
using YoFi.Core.SampleGen;

namespace YoFi.SampleGen;

/// <summary>
/// Processes a sample data configuration, running the generator according to the 
/// configuration stored within.
/// </summary>
public class SampleDataRunner
{
    private Dictionary<string,SampleDataGenerator> Generators = new Dictionary<string, SampleDataGenerator>();

    private List<SampleDataProject> Projects = new List<SampleDataProject>();

    public void Load(SampleDataConfiguration config)
    {
        foreach(var def in config.Definitions)
        {
            using var stream = File.Open(def.Path,FileMode.Open);
            var generator = new SampleDataGenerator();
            generator.LoadDefinitions(stream);
            generator.GenerateTransactions();
            generator.GeneratePayees();
            generator.GenerateBudget();
            Generators[def.Name] = generator;
        }

        Projects.AddRange(config.Projects);
    }

    public void Run(string projectname)
    {
        var project = Projects.Where(x=>x.Name == projectname).Single();
        Run(project);
    }

    public string Run(SampleDataProject project)
    {
        var dirname = "out/" + project.Name;
        Directory.CreateDirectory(dirname);
        foreach(var output in project.Outputs)
        {
            var filenamecomponents = new List<string>()
            {
                "SampleData",
                SampleDataPattern.Year.ToString(),
                output.Load
            };
            if (output.Generate.HasValue)
                filenamecomponents.Add(output.Generate.ToString());
            if (output.Month > 0)
                filenamecomponents.Add("Month" + output.Month.ToString("D2"));

            var filename = dirname + "/" + string.Join('-',filenamecomponents) + $".{output.Save.ToString().ToLowerInvariant()}";

            File.Delete(filename);
            using var stream = File.Open(filename,FileMode.Create);

            var generator = Generators[output.Load];
            generator.Save(stream,action:output.Save,gt:output.Generate,month:output.Month);
        }

        return dirname;
    }
}