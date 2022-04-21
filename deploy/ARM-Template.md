# Azure Resource Manager template

YoFi comes with a pre-configured ARM template to deploy the service to Azure
quickly and easily. Find it at [yofi.azuredeploy.json](/deploy/yofi.azuredeploy.json).

Check out the [ARM Template Docs](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/) for more details on these templates.

## How to deploy it

The easiest method is to simply click the Deploy to Azure button, which launches 
directly into the Azure Portal with the template already loaded. The [documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/)
describes more details on how you can deploy them through the portal, or cloud shell.

[![Deploy To Azure](/docs/images/deploytoazure.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3a%2f%2fraw.githubusercontent.com%2fjcoliz%2fyofi%2fmaster%2fdeploy%2fyofi.azuredeploy.json)

If you're familiar with the az cli tool, you can do it there too. Of course, replace the $ items with proper values for your enviroment.

```Powershell
az deployment group create --resource-group $YoFiResourceGroup --template-file .\deploy\yofi.azuredeploy.json --name "YoFi-$(Get-Random)" --parameters web-user="$YoFiAdminEmail"
```

## What it needs from you

The deployment UI will need a few things from you:

* Which resource group to deploy into. Always create a new resource group. This will make
it easy to clean up all the resources later when you're done.
* Which location to host the resources in. Choose the one closest to you.
* The email address to use for a default account. Once the application is deployed, you'll
need to log in. The email address you give will be used to create a default account.

## Generating secrets

You also have the option to enter values for the various secrets used in deployment. You can
leave the default values alone, to let the template generatee its own secrets for the SQL
Server admin password, for the API access key,
and for the initial account password. 

This is contrary to established best practices. 
Instead, Microsoft recommends that the user always create them. In this case, I'm optimizing for
getting the site up quickly and easily, so the template generates its own. If you choose to use
YoFi with real data over the long term, I recommend deploying with secrets you create yourself. 

## Resources

Here's what it creates:

* App Service. Named 'yofi-{id}', where {id} is a unique identifier for the resource group its created in.
* App Service Plan. Named 'appservice-yofi-{id}'. A server farm where the app service lives.
* Storage Account. Named 'storage{id}'. Used for storing receipts which correspond to transactions.
* SQL Database. Named 'db-yofi-{id}'. Where your data lives.
* SQL Server. Named 'sqlserver-yofi-{id}. Manages connections to your data.
* Application Insights. Named 'insight-yofi-{id}'. For monitoring usage and performance.
* Log Analytics Workspace. Named 'logs-yofi-{id}'. Required for the application insights instance.

The template configures the App Service to load the release package directly from blob storage
every time the site is loaded. While this is convenient for evaluation, you'll definitely want
to take control over your own deployments if you use the app for real.

## Connection Strings

The app service needs connections strings for the database, storage account, and application insights.
The template wires them up onto the app service configuration, so it's ready to go.

## What to do next

At the conclusion of deployment, your site will be ready for you to log in and start using right away!
Here's what you'll need:

* The URL of your new service, e.g. https://yofi-abcdefghijklm.azurewebsites.net
* The username of the initial account
* The password of the initial account

To find the URL of your new service, expand the "deployment details" after the
deployment is complete, then click on the resource of type "Microsoft.Web/sites".

![Deployment complete](/docs/images/postdeploy-1.png)

This takes you to the resource page for your new site. Once there, look for 
the URL. Click the copy icon to copy this to the clipboard. 
Fire up your favorite browser, and paste the link.

![App Service Resource Page](/docs/images/postdeploy-2.png)

After the page loads, you'll be ready to log in. You'll need the email and
password for the default user. 

![Deployment Navigation](/docs/images/postdeploy-3.png)

You can find those again from the deployment 
page. Navigate to "inputs" from the left side.

![Deployment Inputs](/docs/images/postdeploy-4.png)

Then copy the "web-user" and "web-pword" items into the site login.

## Configuration

Read up on [How to configure](/docs/Configuration.md) the site for more details on fine-tuning the configuration.