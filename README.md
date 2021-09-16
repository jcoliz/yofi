# YoFi

Your own finances. Your own code.

YoFi is an open source web-based personal finance manager written in C# on ASP.NET Core.

[![Build Status](https://jcoliz.visualstudio.com/Ofx/_apis/build/status/YoFi%20DotNet?branchName=master)](https://jcoliz.visualstudio.com/Ofx/_build/latest?definitionId=20&branchName=master)
[![Release](https://jcoliz.vsrm.visualstudio.com/_apis/public/Release/badge/c9089da1-9273-4ee3-b0a0-b26a5e1661d7/1/1)](https://jcoliz.vsrm.visualstudio.com/_apis/public/Release/badge/c9089da1-9273-4ee3-b0a0-b26a5e1661d7/1/1)

## Features

## Background

## Try it!

There is a read-only demo instance of YoFi hosted at www.try-yofi.com, with realistic sample data.

## Deploy it

You can add YoFi easily to your Azure subscription by clicking the Deploy button below. 
This will deploy an Azure Resource Manager template to create an Azure Web App, SQL Server,
along with other necessary resources in Azure, then deploy the latest code. 

[![Deploy To Azure](/docs/images/deploytoazure.png)](https://portal.azure.com/#create/Microsoft.Template/)

Don't have an Azure subscription? You can [Create a free account](https://azure.microsoft.com/en-us/free/) in seconds.

The deployment template uses the lowest-cost options for all resources. Only the SQL server instance 
incurs notable cost. At the time of this writing, I'm running perfectly fine on the $5/month Basic tier. 
Azure bills by the minute, so be sure to delete the resources afterward if you're just trying it out.

## Build it

For users of Visual Studio 2019 or higher, it's easy enough to clone the repo, and launch locally.
From there, you can Publish updates from Visual Studio to the Azure App Service instance deployed earlier.

For serious developers, there is an Azure Pipelines definition file included, azure-pipelines-dotnet.yml,
to create an Azure Pipeline to build and release updates to your Azure App Service.