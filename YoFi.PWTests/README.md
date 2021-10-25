# Functional UI Tests in PlayWright

These are browser-driving tests designed to be run against a complete environment
with a seeded database of demo data.

## Current test coverage

Currently, there is a "Smoke test" which makes sure the home page loads, user can log in, and click on all the nav bar items.

## Local Environment

The UI tests are only designed to work on a local machine, right now.

## Future: Test Environment

In the future, I have ambition to run them as part of the release pipeline. 
In this case, I will deploy a fresh environment (which will also test the ARM Template), then deploy the
bits under test to it, and run these tests.

## Dependencies

Uses [Playwright for .NET](https://github.com/microsoft/playwright-dotnet). 
It uses DevTools, which seems like a more modern and stable approach than WebDriver.
Playwright seems easier and more stable to set up as well.
And there are MSTest helpers out of the box, so it's all very clean.

## How to use it

### First, run the code

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

### Second, run the tests

Either run them in Visual Studio, or in another command line window

```
PS> dotnet test
  Determining projects to restore...
  All projects are up-to-date for restore.
  YoFi.PWTests ->.\YoFi.PWTests\bin\Debug\netcoreapp3.1\YoFi.PWTests.dll
Test run for .\YoFi.PWTests\bin\Debug\netcoreapp3.1\YoFi.PWTests.dll (.NETCoreApp,Version=v3.1)
Microsoft (R) Test Execution Command Line Tool Version 16.11.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     7, Skipped:     0, Total:     7, Duration: 5 s - YoFi.PWTests.dll (netcoreapp3.1)
```

This is an experimental project to write UI tests in PlayWright.

## Steps to reproduce

Here's what I did to get here

1. Created this project:

```
PS> dotnet new mstest -f netcoreapp3.1
```

2. Updated packages to latest versions in csproj

3. Installed MStest helpers:

```
PS> dotnet add package Microsoft.Playwright.MSTest
```

4. Installed tools:

```
PS> dotnet tool install --global Microsoft.Playwright.CLI
You can invoke the tool using the following command: playwright
Tool 'microsoft.playwright.cli' (version '1.2.0') was successfully installed.
```

5. Installed dependencies:

```
PS> playwright install
Playwright build of chromium v930007 downloaded to ~\AppData\Local\ms-playwright\chromium-930007
Playwright build of ffmpeg v1006 downloaded to ~\AppData\Local\ms-playwright\ffmpeg-1006
Playwright build of firefox v1297 downloaded to ~\AppData\Local\ms-playwright\firefox-1297
Playwright build of webkit v1564 downloaded to ~\AppData\Local\ms-playwright\webkit-1564
```

6. In another session, launched the app

7. Recorded a simple session with login and run through pages. Very handy!!

```
PS> playwright codegen -o login.cpp http://localhost:50419
```

8. Then created SmokeTest.cs with those various steps.

## References

* [PlayWright: Getting Started](https://playwright.dev/docs/intro)
* [PlayWright for .NET](https://github.com/microsoft/playwright-dotnet)

