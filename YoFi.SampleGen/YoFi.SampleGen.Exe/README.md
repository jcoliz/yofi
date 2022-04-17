# Sample Generator

This console app will create the needed sample data.
Previously, I used outputs from various tests as the sample data files.
It was pretty scattershot.
So here with the command-line utility, I bring it into a single regime.

The console app needs to create a single consistent set of data. That is, it should only call the
Generate() methods ONCE. This is an important distinction versus the tests, as the tests call
Generate() methods many times for test purposes.

## Usage

```
YoFi.SampleGen.Exe: Generate sample data for project
  -y, --year=YEAR            Calendar YEAR for all data.
                               Defaults to current year.
  -p, --project=PROJECT      Which PROJECT to generate data for.
                               Repeat this option for multiple projects.
                               Generates for all projects if not specified.
  -i, --inplace              Generate data in its final location.
                               Otherwise generates to local directory.
  -h, --help                 Show this message and exit.

Available Projects:
        YoFi.AspNet
        YoFi.Data
        YoFi.Tests
        YoFi.Core.Tests.Unit
        YoFi.Tests.Integration
        YoFi.Tests.Functional
```

## Example Run

```
PS> dotnet run -- -i

YoFi.SampleGen.Exe: Generate sample data for project
        Run with --help for details
> YoFi.AspNet
        ../../YoFi.AspNet/wwwroot/sample/SampleData-2022-Full.xlsx
> YoFi.Data
        ../../YoFi.Data/SampleData/SampleData-2022-Full.json
> YoFi.Tests
        ../../YoFi.Tests/SampleData/SampleData-2022-Full.json
        ../../YoFi.Tests/SampleData/SampleData-2022-Full.xlsx
        ../../YoFi.Tests/SampleData/SampleData-2022-Full-Month02.ofx
> YoFi.Core.Tests.Unit
        ../../YoFi.Core.Tests.Unit/SampleData/SampleData-2022-Full.json
        ../../YoFi.Core.Tests.Unit/SampleData/SampleData-2022-Full-Month02.ofx
> YoFi.Tests.Integration
        ../../YoFi.Tests.Integration/SampleData/SampleData-2022-Full.json
        ../../YoFi.Tests.Integration/SampleData/SampleData-2022-Full-Month02.ofx
        ../../YoFi.Tests.Integration/SampleData/SampleData-2022-Upload.xlsx
> YoFi.Tests.Functional
        ../../YoFi.Tests.Functional/SampleData/SampleData-2022-Full-Month01.ofx
        ../../YoFi.Tests.Functional/SampleData/SampleData-2022-Upload.xlsx
        ../../YoFi.Tests.Functional/SampleData/SampleData-2022-Upload-Tx.xlsx
```

## Configuration

The configuration of which projects get which kind of data is all contained in the
[SampleDataConfiguration.json](./SampleDataConfiguration.json) file.
