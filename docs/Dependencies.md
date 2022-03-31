# Dependencies

## Microsoft Foundational Components

### .NET 6

Fundamentally the app is built on [.NET 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0). 
It can run on Linux or Windows.
My policy is to be always on the latest LTS version.

### Entity Framework Core

For Object-Relational Mapping. It writes better SQL queries than I could dream of.
It's amazing.

### MS SQL Server

Currently the only supported database is MS SQL Server. Support for MySQL is on the roadmap.

## Services

### Azure Storage SDK

For storing/retrieving receipts as blobs in Azure Storage (optional)

### SendGrid

For sending emails (optional)

## Packages

### OfficeOpenXMLEasy
https://github.com/jcoliz/OfficeOpenXMLEasy

For serializing/deserializing objects to/from spreadsheet files. Uses Office Open XML SDK.

### OFX Sharp
https://github.com/jcoliz/OFXSharp

For reading OFX files.

### Excel Financial Functions
https://github.com/fsprojects/ExcelFinancialFunctions/

For calculating loan principal & interest breakdown.

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