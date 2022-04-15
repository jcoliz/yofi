# Sample Generator

This console app will create the needed sample data. Note that currently, this is all done by the tests.
Moving that to the console app is a TODO.

The console app needs to create a single consistent set of data. That is, it should only call the
Generate() methods ONCE. This is an important distinction versus the tests, as the tests call
Generate() methods many times for test purposes.

## Usage

```
    YoFi.SampleGen.Exe <Input> --year <Year> --project <Project> --inplace --help
```

## Input

* Sample Data Definitions, e.g. YoFi.SampleGen.Tests/SampleData/FullSampleDataDefinition.xlsx

If input is not specified, it will look for FullSampleDataDefinition.xlsx in the current
directory.

## Options

* --year &lt;Year&gt;. Defaults to current year
* --project &lt;Project&gt;. Output only single project. Ok to have multiple --output options for multiple projects. If not specified, defaults to generating all projects.
* --inplace. Generate outputs in their correct directories in the project. If not included, will generate in "out" under current directory
* --help. Show usage and options

The "Project" output is the name of a single project in the YoFi top-level. e.g. "YoFi.AspNet" or "YoFi.Data" for production,
or e.g. "YoFi.Core.Tests.Unit" for that test project.

## Output: Production Data

Note this really only needs to be done once a year, or if a change is needed

### YoFi.AspNet

In wwwroot/sample:

* SampleData-Full.xlsx 

### YoFi.Data

In SampleData:

* FullSampleData.json

NOTE: When the refactoring of YoFi.Data is complete, the sample data in wwwroot
should no longer be needed.

## Output: Test Collateral

This should never be needed, unless there's a new feature added to sample data

### YoFi.Tests

In SampleData:

* SampleData-Full.xlsx
* FullSampleData.json
* FullSampleData-Month02.ofx

NOTE: When the refactoring of YoFi.Data is complete, these tests should be 
reconsidered to see if sample data is still needed there.

### YoFi.Core.Tests.Unit

In SampleData:

* FullSampleData.json
* FullSampleData-Month02.ofx

### YoFi.Tests.Integration

In SampleData:

* FullSampleData.json
* FullSampleData-Month02.ofx
* Test-Generator-GenerateUploadSampleData.xlsx

NOTE: When the refactoring of YoFi.Data is complete, its possible that this may
not need FullSampleData.json, as it can use the copy in YoFi.Data.

### YoFi.Tests.Functional

In SampleData:

* FullSampleData-Month01.ofx
* Test-Generator-GenerateUploadSampleData.xlsx
* Test-Generator-GenerateUploadSampleData-Transactions.xlsx