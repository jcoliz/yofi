# Sample Generator

This console app will create the needed sample data. Note that currently, this is all done by the tests.
Moving that to the console app is a TODO.

## Inputs

* Sample Data Definitions, e.g. YoFi.SampleGen.Tests/SampleData/FullSampleDataDefinition.xlsx

## Options

* Year
* Which output(s) to generate

## Output: Production Data

Note this really only needs to be done once a year, or if a change is needed

In YoFi.AspNet/wwwroot/sample:

* SampleData-Full.xlsx 

## Output: Test Collateral

This should never be needed, unless there's a new feature added to sample data

### Unit Tests

In YoFi.Tests/SampleData:

* SampleData-Full.xlsx
* FullSampleData.json
* FullSampleData-Month02.ofx

### Integration Tests

In YoFi.Tests.Integration/SampleData:

* FullSampleData.json
* FullSampleData-Month02.ofx
* Test-Generator-GenerateUploadSampleData.xlsx

### Functional Tests

In YoFi.Tests.Functional/SampleData:

* FullSampleData-Month01.ofx
* Test-Generator-GenerateUploadSampleData.xlsx
* Test-Generator-GenerateUploadSampleData-Transactions.xlsx