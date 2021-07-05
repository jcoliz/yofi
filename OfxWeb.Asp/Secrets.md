# Secrets

This app uses certain secrets. Setting these secrets is not required for normal operation. If the secrets are not present, functionality which uses the secret will be disabled.

See [Safe storage of app secrets in development in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets).

## Access to secrets

### In development

Microsoft recommends using the "Secret Manager" to store secrets in development. This is what I use. You could also use
environment variables. Either way, nothing special is required in code to access secrets during development. The
secrets show up as configuration variables in the usual way.

### In production

There are multiple was to get secrets in production. This app is configured to use the [Azure Key Vault Configuration Provider in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration),
if you want to use that. If so, set the "KEYVAULTURL" environment variable in your production environment the 
fully-qualified keyvault URL, e.g. "https://myspecialkeys.vault.azure.net/".

## Specific Secrets Used

### Api:Key

The Api:Key secret is used to authenticate callers to certain API calls. 

If this key is missing, those API calls will fail.

### AdminUser

The keys AdminUser:Email and AdminUser:Password are used to deploy an administrative user, typically on first launch.

If this section is missing, such a user will not be created.

### AzureStorage

This section is used to construct a connection string to Azure Blob Storage. You\'ll need to set AzureStorage:AccountName and AzureStorage:AccountKey.

If this section is missing, blob storage features will not be available. Currently that\'s uploading receipts.
