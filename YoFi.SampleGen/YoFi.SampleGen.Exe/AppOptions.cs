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
        Add( "y|year=", "calendar {YEAR} for all data", (int v) => Year = v);
        Add( "p|project=", "which {PROJECT} to generate data for", v => Projects.Add(v));
        Add( "i|inplace", "generate data in its final location", v => InPlace = v != null);
        Add( "h|help", "show this message and exit", v => Help = v != null );
    }
}