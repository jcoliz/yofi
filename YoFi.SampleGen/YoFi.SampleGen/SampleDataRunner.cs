using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace YoFi.SampleGen;

/// <summary>
/// Processes a sample data configuration, running the generator according to the 
/// configuration stored within.
/// </summary>
public class SampleDataRunner
{
    public Definition[] Definitions { get; set; }
    public SampleDataProject[] Projects { get; set; }

    private Dictionary<string,SampleDataGenerator> Generators = new Dictionary<string, SampleDataGenerator>();

    public void Load()
    {
        foreach(var def in Definitions)
        {
            using var stream = File.Open(def.Path,FileMode.Open);
            var generator = new SampleDataGenerator();
            generator.LoadDefinitions(stream);
            generator.GenerateTransactions();
            generator.GeneratePayees();
            generator.GenerateBudget();
            Generators[def.Name] = generator;
        }
    }

    public IEnumerable<string> Run(SampleDataProject project)
    {
        var result = new List<string>();
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
            result.Add(filename);

            File.Delete(filename);
            using var stream = File.Open(filename,FileMode.Create);

            var generator = Generators[output.Load];
            generator.Save(stream,action:output.Save,gt:output.Generate,month:output.Month);
        }

        return result;
    }
}