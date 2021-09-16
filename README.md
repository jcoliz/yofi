# YoFi

Your own finances. Your own code.

YoFi is an open source web-based personal finance manager written in C# on ASP<meta>.NET Core. Host it on a cloud service to enable access to your personal finances from any device anywhere anytime, while running on hardware you control. 

[![Build Status](https://jcoliz.visualstudio.com/Ofx/_apis/build/status/YoFi%20DotNet?branchName=master)](https://jcoliz.visualstudio.com/Ofx/_build/latest?definitionId=20&branchName=master)
[![Release](https://jcoliz.vsrm.visualstudio.com/_apis/public/Release/badge/c9089da1-9273-4ee3-b0a0-b26a5e1661d7/1/1)](https://jcoliz.vsrm.visualstudio.com/_apis/public/Release/badge/c9089da1-9273-4ee3-b0a0-b26a5e1661d7/1/1)

## Features

YoFi's basic concept is to let you import bank & credit card statements, categorize each
transaction, then create reports based on those categories. Its full feature set includes:

* Creating and tracking budgets on a yearly or monthly basis
* Automatic assignment of categories based on the payee for a transaction, using either partial text match or regular expressions
* Super flexible reporting system
* Importing OFX files downloaded from your bank
* Importing from and exporting to XLSX spreadsheets for all data types 
* Access to all reports via REST API
* Attaching receipt images to individual transactions
* Bulk reassignment of categories
* Unlimited levels of subcategories

## Background

What's the best way to manage your money? I always thought Microsoft Excel was the answer to this
question. It's powerful enough to do whatever you want, and set things up however you need. After
years of this, though, I found it cumbersome to consistently maintain all the structure I wanted.

Thus, YoFi was born of my desire to have a transaction manager work just the way I wanted. C#, ASP<meta>.NET and SQL Server are probably still yet more powerful than Excel, so I could have everything work the
way I want, and not have to keep maintaining complex spreadsheets.

From time to time, people have asked what I use to manage my income & expenses. When I say, "I wrote
my own," the next question is usually, "Can I see the code?" So, here's the code!

If you're a developer, you may find it useful to have a codebase which you can alter to work however
you like. Even if you're not a developer, you may find it interesting to have your personal finance
manager accessible from any device anywhere anytime, yet still running on hardware you control.

## Try it!

There is a read-only demo instance of YoFi hosted at www.try-yofi.com, with realistic sample data.

## Deploy it

The fastest and simplest way to bring up YoFi for your own use is to deploy it to Azure.
Click the Deploy button below to deploy the [Azure Resource Manager template](/deploy/ARM-Template.md).
This creates all the needed resources in Azure, including an App Service, SQL Database, 
Storage, etc., then deploys the most recent build. 

[![Deploy To Azure](/docs/images/deploytoazure.png)](https://portal.azure.com/#create/Microsoft.Template/)

Don't have an Azure subscription? You can easily [Create a free account](https://azure.microsoft.com/en-us/free/).

The deployment template uses the lowest-cost options for all resources. Only the SQL server instance 
incurs notable cost. At the time of this writing, the $5/month Basic tier works great for our family's
use.

Azure bills you for only the minutes you use, so be sure to delete the resources afterward if you're just trying it out.

## Build it

For users of Visual Studio 2019 or higher, it's easy enough to clone the repo, and launch locally.
From there, you can Publish updates from Visual Studio to the Azure App Service instance deployed earlier.

For serious developers, there is an Azure Pipelines definition file included, [azure-pipelines-dotnet.yml](/azure-pipelines-dotnet.yml). You can host your build
service on Azure Dev Ops, then create an Azure Pipeline to build and release updates to your Azure App Service.

## Give feedback

I'd love to hear what you think! If you'd like to see something change, please feel free to open an issue. Or just send me a mail!