# Functional UI Tests for Asp.Net Application using PlayWright

These are browser-driving tests designed to be run against a complete environment
with a seeded database of demo data.

## Test coverage

My vision is that there is exactly a 1:1 correspondence between User Stories and individual functional test.
That is, for everything we promise the user they can do, we have a test to prove it can be done.

Lately I have taken to marking such user-facing User Stories as [User Can] stories in Azure Dev Ops.

## Local Environment

The UI tests are only designed to work on a local machine, right now.

## Secrets

The tests pull secrets from the local development secret storage, using the same project ID as
the main page. So, set these secrets, and you'll guarantee consistency between the app and the
tests:
* AdminUser:Email
* AdminUser:Password
* Api:Key

## Future: Test Environment

In the future, I have ambition to run them as part of the release pipeline. 
In this case, I will deploy a fresh environment (which will also test the ARM Template), to match the same settings
as a production environment.
Next it will deploy the
bits under test to it, and run these tests.

## Dependencies

Uses [Playwright for .NET](https://github.com/microsoft/playwright-dotnet). 
This is built on DevTools, which seems like a more modern and stable approach than WebDriver.
Playwright seems easier and more stable to set up as well.
And there are MSTest helpers out of the box, so it's all very clean.

## Principles

### Click path only

We never ever navigate to a specific URL in the tests, except the top of the site. Absolutely every test
represents a click path from the top of the site. The reason for this is that users typically don\'t interact 
with their address bar other than to enter the top of the site. Users navigate by clicking around, so should
the test.

### No dependency on code under test

Unlike with unit tests or integration tests, there is a principle here that the functional test
do not load the app code at all.

## How to use it

### Environment vars

The tests expect that the system under test is running with the following environment var setup

```PowerShell
$env:Demo__IsEnabled = "false"
$env:Storage__BlobContainerName = "yofi-uitest"
$env:Clock__Now = "2022-12-31"
$env:ConnectionStrings__DefaultConnection = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=yofi-test-functional;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"
```

### Set up a dedicated LocalDB

It's best to isolate functional tests into their own LocalDB database, so they don't mix with data in your
primary development instance. I created a database called "yofi-test-functional" using the SQL Server
Object Explorer in Visual Studio. Then, I copied the Connection String out of the Properties pane for
that database. That connection string needs to go in startbg.ps1 and runtests.ps1.

### Be aware of the clock

The functional tests expect all transactions to be visible, not hidden. However, for public demo purposes, the
demo hides future transactions so to look more realistic. The solution is to set the Clock:Now configuration
to 12-31-{year}. This will ensure all transactions are visible.

### Use the runtests.ps1 script

This test builds the app code, runs it in the background, then builds and runs the functional tests

```
PS .\YoFi.Tests.Functional> .\runtests.ps1
Microsoft (R) Build Engine version 16.11.0+0538acc04 for .NET
Copyright (C) Microsoft Corporation. All rights reserved.

  Determining projects to restore...
  All projects are up-to-date for restore.
  OfficeOpenXmlEasy -> .\submodules\OfficeOpenXMLEasy\src\bin\Debug\netstandard2.1\OfficeOpenXmlEasy.dll
  YoFi.Tests.Functional -> .\YoFi.Tests.Functional\bin\Debug\netcoreapp3.1\YoFi.Tests.Functional.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.12
Microsoft (R) Build Engine version 16.11.0+0538acc04 for .NET
Copyright (C) Microsoft Corporation. All rights reserved.

  Determining projects to restore...
  All projects are up-to-date for restore.
  OfficeOpenXmlEasy -> .\submodules\OfficeOpenXMLEasy\src\bin\Debug\netstandard2.1\OfficeOpenXmlEasy.dll
  OfxSharp -> .\submodules\OFXSharp\source\OfxSharp\bin\Debug\netstandard2.0\OfxSharp.dll

  Bundler: Begin processing bundleconfig.json
  Bundler: Done processing bundleconfig.json
  YoFi.AspNet -> .\YoFi.AspNet\bin\Debug\netcoreapp3.1\YoFi.AspNet.dll
  YoFi.AspNet -> .\YoFi.AspNet\bin\Debug\netcoreapp3.1\YoFi.AspNet.Views.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.17

Id     Name            PSJobTypeName   State         HasMoreData     Location             Command
--     ----            -------------   -----         -----------     --------             -------
7      uitests         BackgroundJob   Running       True            localhost             dotnet run

  Determining projects to restore...
  All projects are up-to-date for restore.
  OfficeOpenXmlEasy -> .\submodules\OfficeOpenXMLEasy\src\bin\Debug\netstandard2.1\OfficeOpenXmlEasy.dll
  YoFi.Tests.Functional -> .\YoFi.Tests.Functional\bin\Debug\netcoreapp3.1\YoFi.Tests.Functional.dll

Test run for C:\Source\jcoliz\Ofx\YoFi.Tests.Functional\bin\Debug\netcoreapp3.1\YoFi.Tests.Functional.dll (.NETCoreApp,Version=v3.1)
Microsoft (R) Test Execution Command Line Tool Version 16.11.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:   123, Skipped:     0, Total:   123, Duration: 2 m 35 s - YoFi.Tests.Functional.dll (netcoreapp3.1)
```

### Or, run the code first

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

### Then run the tests

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

Passed!  - Failed:     0, Passed:   123, Skipped:     0, Total:   123, Duration: 2 m 35 s - YoFi.Tests.Functional.dll (netcoreapp3.1)
```

## Viewport size options

It can be useful to change the viewport being used for tests. This has two values:
* Screen shots. The tests take a lot of screen shots, so it can be super helpful to see how the app is looking overall, or review a single feature on various screen sizes
* Functionality differences. Various viewports hide or show different things, so it's good to ensure everything is still working on all viewports.

In runsettings, the "viewportSize" parameter is used to control the size of viewport used when testing. The options are:

* "wide" 1080 x 810. That's iPad Landscape, which is the default visual tuning for this app.
* "tablet" 810 x 1080. iPad Portrait.
* "phone" 390 x 844. iPhone 12 
* "default": Currently equivalent to "wide"

You can change this in the runsettings file, or override on the command line:

```
dotnet test --% -- TestRunParameters.Parameter(name=\"viewportSize\", value=\"phone\")
```

## Steps to reproduce

Here's what I did to get here:

### 1. Created this project

```
PS> dotnet new mstest -f netcoreapp3.1
```

### 2. Updated packages to latest versions in csproj

```diff
--- a/YoFi.PWTests/YoFi.PWTests.csproj
+++ b/YoFi.PWTests/YoFi.PWTests.csproj
@@ -7,10 +7,13 @@
   </PropertyGroup>

   <ItemGroup>
-    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="16.9.4" />
-    <PackageReference Include="MSTest.TestAdapter" Version="2.2.3" />
-    <PackageReference Include="MSTest.TestFramework" Version="2.2.3" />
-    <PackageReference Include="coverlet.collector" Version="3.0.2" />
+    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="16.11.0" />
+    <PackageReference Include="MSTest.TestAdapter" Version="2.2.7" />
+    <PackageReference Include="MSTest.TestFramework" Version="2.2.7" />
+    <PackageReference Include="coverlet.collector" Version="3.1.0">
+      <PrivateAssets>all</PrivateAssets>
+      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
+    </PackageReference>
   </ItemGroup>

 </Project>
```

### 3. Installed MStest helpers

```
PS> dotnet add package Microsoft.Playwright.MSTest
```

### 4. Installed tools

```
PS> dotnet tool install --global Microsoft.Playwright.CLI
You can invoke the tool using the following command: playwright
Tool 'microsoft.playwright.cli' (version '1.2.0') was successfully installed.
```

### 5. Installed dependencies

```
PS> playwright install
Playwright build of chromium v930007 downloaded to ~\AppData\Local\ms-playwright\chromium-930007
Playwright build of ffmpeg v1006 downloaded to ~\AppData\Local\ms-playwright\ffmpeg-1006
Playwright build of firefox v1297 downloaded to ~\AppData\Local\ms-playwright\firefox-1297
Playwright build of webkit v1564 downloaded to ~\AppData\Local\ms-playwright\webkit-1564
```

### 6. In another session, launched the app

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

### 7. Recorded a simple session with login and run through pages. Very handy!!

Command:

```
PS> playwright codegen -o login.cpp http://localhost:50419
```

Resulting generated code:

```c#
class Program
{
    public static async Task Main()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,
        });
        var context = await browser.NewContextAsync();

        // Open new page
        var page = await context.NewPageAsync();

        // Go to http://localhost:50419/
        await page.GotoAsync("http://localhost:50419/");
    }
}
```

### 8. Then created SmokeTest.cs with those various steps.

```c#
[TestClass]
public class SmokeTest: PageTest
{
    private readonly string Site = "http://localhost:50419/";

    [TestMethod]
    public async Task AAA_HomePage()
    {
        // Given: An empty context, where we are not logged in
        // (This is accomplished by ordering this test before the login test)

        // When: Navigating to the root of the site
        await Page.GotoAsync(Site);

        // Then: The home page loads
        var title = await Page.TitleAsync();
        Assert.AreEqual("Home - Development - YoFi", title);
    }
}
```

## References

* [PlayWright: Getting Started](https://playwright.dev/docs/intro)
* [PlayWright for .NET](https://github.com/microsoft/playwright-dotnet)