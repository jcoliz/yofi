using Mono.Options;

namespace YoFi.SampleGen.Exe;

public class AppOptions: OptionSet
{
    public bool Help { get; private set; }
    public int? Year { get; private set; }
    public List<string> Projects { get; private set; } = new List<string>();
    public bool InPlace { get; private set; }

    public AppOptions()
    {
        Add( "y|year=", "Calendar {YEAR} for all data.\nDefaults to current year.", (int v) => Year = v);
        Add( "p|project=", "Which {PROJECT} to generate data for.\nRepeat this option for multiple projects.\nGenerates for all projects if not specified.", v => Projects.Add(v));
        Add( "i|inplace", "Generate data in its final location.\nOtherwise generates to local directory.", v => InPlace = v != null);
        Add( "h|help", "Show this message and exit.", v => Help = v != null );
    }
}