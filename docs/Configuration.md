# Configuration

Here's how to configure YoFi.
Only the SQL server connection string is absolutely required. 
If others are missing, the application will simply exclude the features enabled by the missing configuration.

## Where to store Configuration Keys

You have three choices on where you want to store your keys:

1. Environment variables. Both in production and during development, you may set these keys using environment variables. For configuration
keys which are not particularly sensitive, like the site name, this is the best choice. Keys are listed here of the form "Section:Key". To
add them to your environment, the environment variable name replaces the colon with two underscores, e.g. "SECTION__KEY".
2. Azure Key Vault. In production only, you may choose to store sensitive configuration keys in the "Secrets" section of an Azure Key Vault. In this case,
replace the colon with two dashes, e.g. "Section--Key".
3. Secret Manager. Microsoft recommends using the "Secret Manager" to store secrets in development. 

For more information on storing app secrets, please 
see [Safe storage of app secrets in development in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets).

## Connected Services

The application leverages several other services. Connection strings for these services should be set in the Configuration tab of the App Service, in the "Connection Strings" section

![Connection Strings Configuration](/docs/images/connection-strings.png)

### SQL Server

REQUIRED. The "DefaultConnection" connection string must be the connection string to SQL Server. The [ARM deployment template](/deploy/ARM-Template.md) automatically creates an Azure-hosted SQL Server instance, and add sthe connection string to the App Service configuration. 

### Azure Storage

The "StorageConnection" connection string should be a connection string to your Azure Storage account. Azure Storage is currently only used for storing transaction receipts. If this is missing, storing and retrieving receipts will be unavailable. The [ARM deployment template](/deploy/ARM-Template.md) automatically creates an Azure Storage account, and add sthe connection string to the App Service configuration. 

### Azure Key Vault

The "KeyVaultConnection" connection string may optionally contain the fully-qualified URI of your Azure Key Vault service, e.g. "https://myspecialkeys.vault.azure.net/". You may choose to set up an Azure Key Vault to hold any of the configuration properties described here, other than connection strings. 
This may be useful for sensitive keys such as the Administrator User Account password or the API Key.

This setting is only useful in production. It requires that the app be running as an App Service, and that you've given the app permission
to access your key vault. Be sure to read more about the [Azure Key Vault Configuration Provider in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration) if you choose to go this route.

### Application Insights

If you are running an App Insights instance, put the connection string in ApplicationInsights:ConnectionString. Read how to [Create an Application Insights resource](https://docs.microsoft.com/en-us/azure/azure-monitor/app/create-new-resource), and how to use its [Connection strings](https://docs.microsoft.com/en-us/azure/azure-monitor/app/sdk-connection-string).

## Access

### Administrator User Account

The keys AdminUser:Email and AdminUser:Password are used to deploy an administrative user, typically on first launch. If these keys are missing, such a user will not be created. Instead, you'll need to create an account the usual way, then log into SQL Server directly to assign it to the "Administrator" role.

The [ARM deployment template](/deploy/ARM-Template.md) automatically adds these keys to the App Service configuration environment variables.

### API Key

The Api:Key key  is used to authenticate callers to certain API calls. If this key is missing, those API calls will fail. The [ARM deployment template](/deploy/ARM-Template.md) automatically generates an API Key and adds it to the App Service configuration environment variables. You may choose to move it to
an Azure Key Vault for improved security.

## E-Mail

YoFi can use SendGrid to send emails on your behalf if you have an account set up. This is helpful if you want to allow others to
create accounts and validate their email. If you're just using it yourself with the initial administrator account, this is not needed.

### SendGrid:Key

The API key issued by SendGrid for access.

### SendGrid:Email

The email address from which the emails are sent.

### SendGrid:Sender

The plain-language sender name that the emails will appear to be from.

## Branding

These keys control how the brand of your specific site is presented. It is not required to set them. Default codebase branding will be used if these are missing.

### Brand:Name

The simple brandname of your site. e.g. "YoMoney"

### Brand:Icon

The definition for your icon, using font-awesome, e.g. "fas fa-comment-dollar"

### Brand:Link

Url where you host your site, e.g. "https://www.yomoney.net/"

### Brand:Owner

Name of company or individual responsible for your site.

## Demo

These keys control how much of demonstration this version is, vs full production.

### Demo:IsEnabled

False by default, but set true for www.try-yofi.com. This seeds the database automatically, as well adds
extra help text and simplifies the click path for evaluating users. 

### Demo:IsHomePageRoot

True by default. Indicates that https://{site}/ will show the Home page. Set this to "false" to bypass that
and go straight to your transactions list.

## Clock

### Clock:Now

Useful during development if you want to force the system to think it's a certain day.