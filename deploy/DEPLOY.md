# How to Deploy

## Azure Resource Manager

The fastest and simplest way to bring up YoFi for your own use is to deploy it to Azure.
Click the Deploy button below to deploy the [Azure Resource Manager template](/deploy/ARM-Template.md).
This creates all the needed resources in Azure, including an App Service, SQL Database, 
Storage, etc., then deploys the most recent build. 

[![Deploy To Azure](/docs/images/deploytoazure.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3a%2f%2fraw.githubusercontent.com%2fjcoliz%2fyofi%2fmaster%2fdeploy%2fyofi.azuredeploy.json)

Don't have an Azure subscription? You can easily [Create a free account](https://azure.microsoft.com/en-us/free/).

## Hosting Provider

I am also planning to deploy to a regular hosting provider. I'll report back how that goes here!

## What's next?

After deploying, read up on [How to configure](/docs/Configuration.md) the site.