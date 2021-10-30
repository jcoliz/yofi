# Dependencies

## Microsoft Foundational Components

### ASP<meta>.NET Core 3.1 LTS

Fundamentally the app is built on .NET Core. It can run on Linux or Windows. My policy is to be
on the latest LTS version, which is 3.1.19 as of this writing. The next LTS is predicted to be
.NET 6.0 in November 2021. At that point, I'll upgrade to .NET 6.0.

### Entity Framework Core

For Object-Relational Mapping. It writes better SQL queries than I could dream of.
It's amazing.

### MS SQL Server

Currently the only supported database is MS SQL Server. Support for MySQL is on the roadmap.

### Azure Storage SDK

For storing/retrieving receipts as blobs in Azure Storage.

## Submodules

### jcoliz/OfficeOpenXMLEasy
https://github.com/jcoliz/OfficeOpenXMLEasy

For serializing/deserializing objects to/from spreadsheet files. Uses Office Open XML SDK.

### OFX Sharp
https://github.com/jcoliz/OFXSharp

For parsing OFX files. I am consuming this as a submodule because the mainline
OFX Sharp is compiled for full .NET Framework. Current development targets .NET Standard 2.0.

## Infrastructure

### Playwright.NET

For browser-based functional testing. Playwright is super stable and easy to use. I like having
functional tests running against the final deployed bits to ensure there aren't any bugs
hiding somewhere in the configuration or infrastructure.

## Client-side libraries

### Bootstrap 5

For looking at least half-way decent.

### JQuery

For AJAX.

### Font Awesome Free

For nice icons. Using the free version to avoid tying an open-source project to a 'premium' offering.