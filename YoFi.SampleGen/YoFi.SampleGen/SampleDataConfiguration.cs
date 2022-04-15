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
    public SampleDataGenerator.SaveOptions Save { get; set; }
}

// Outputs to filename:
//
// SampleData-{Load}(-{Generate})(-Month{Month}).{Save}
//
// SampleData-Full.json
// SampleData-Full.xlsx
// SampleData-Upload.xlsx
// SampleData-Upload-Tx.xlsx
// SampleData-Full-Month02.ofx
//
// Outputs to directory (local)
//
// {Name}
//
// Outpus to directory (in place)
//
// [Root]/{Name}/{Path}