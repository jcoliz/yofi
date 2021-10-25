# Functional UI Tests in PlayWright

This is an experimental project to write UI tests in PlayWright.

## Steps to reproduce

Here's what I did.

Created this project:

```
PS> dotnet new mstest -f netcoreapp3.1
```

Updated packages to latest versions in csproj

Installed MStest helpers:

```
PS> dotnet add package Microsoft.Playwright.MSTest
```

Installed tools:

```
PS> dotnet tool install --global Microsoft.Playwright.CLI
You can invoke the tool using the following command: playwright
Tool 'microsoft.playwright.cli' (version '1.2.0') was successfully installed.
```

Installed dependencies:

```
PS> playwright install
Playwright build of chromium v930007 downloaded to C:\Users\JamesColiz\AppData\Local\ms-playwright\chromium-930007
Playwright build of ffmpeg v1006 downloaded to C:\Users\JamesColiz\AppData\Local\ms-playwright\ffmpeg-1006
Playwright build of firefox v1297 downloaded to C:\Users\JamesColiz\AppData\Local\ms-playwright\firefox-1297
Playwright build of webkit v1564 downloaded to C:\Users\JamesColiz\AppData\Local\ms-playwright\webkit-1564
```

In another session, launched the app

Recorded a simple session with login and run through pages

```
PS> playwright codegen -o login.cpp http://localhost:50419
```

## References

* [PlayWright: Getting Started](https://playwright.dev/docs/intro)
* [PlayWright for .NET](https://github.com/microsoft/playwright-dotnet)

