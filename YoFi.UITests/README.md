# Functional UI Tests

These are browser-driving tests designed to be run against a complete environment
with a seeded database of demo data.

They are just getting started, so there's not a ton here yet.

## Local Environment

Currently, the UI tests are only designed to work on a local machine.

## Test Environment

In the future, I have
ambition to run them as part of the Azure Pipelines release pipeline. In this case, I
will deploy a fresh environment (which will also test the ARM Template), then deploy the
bits under test to it, and run these tests.

## Dependencies

Currently using Selenium. Currently uses Chrome browser only, and is hard-coded to a specific version (see .csproj). In the future,
this can be reworked to match the currently-installed version.

Considering moving to  [Playwright for .NET](https://github.com/microsoft/playwright-dotnet) as a next step, because DevTools protocol looks more
modern and stable than WebDriver.

## First, run the code

```
PS> dotnet run
**************************************************************
**                                                          **
** APPLICATION STARTUP                                      **
**                                                          **
**************************************************************
** Version: fbb7ca19                                        **
Hosting environment: Development
Content root path: .\YoFi.AspNet
Now listening on: http://localhost:50419
Application started. Press Ctrl+C to shut down.
```

## Second, run the tests

Either run them in Visual Studio, or in another command line window

```
PS> dotnet test
  Determining projects to restore...
  All projects are up-to-date for restore.
  YoFi.UITests -> .\YoFi.UITests\bin\Debug\netcoreapp3.1\YoFi.UITests.dll
Test run for .\YoFi.UITests\bin\Debug\netcoreapp3.1\YoFi.UITests.dll (.NETCoreApp,Version=v3.1)
Microsoft (R) Test Execution Command Line Tool Version 16.11.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 789 ms - YoFi.UITests.dll (netcoreapp3.1)
```