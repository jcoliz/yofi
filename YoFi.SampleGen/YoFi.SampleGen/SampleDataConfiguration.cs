namespace YoFi.SampleGen;

public class SampleDataConfiguration
{
    public Definition[] Definitions { get; set; }
    public SampleDataProject[] Projects { get; set; }
}

public class Definition
{
    public string Name { get; set; }
    public string Path { get; set; }
}

public class SampleDataProject
{
    public string Name { get; set; }

    public string Path { get; set; }

    public SampleDataOutput[] Outputs { get; set; }
}

public class SampleDataOutput
{
    public string Load { get; set; }
    public enum GenerateType { Full, Tx };
    public GenerateType? Generate { get; set; }
    public enum SaveType { Xlsx, Json, Ofx };
    public SaveType Save { get; set; }
    public int Month { get; set; }
}
