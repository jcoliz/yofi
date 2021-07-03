# Secrets

This app uses certain secrets. Setting these secrets is not required for normal operation. If the secrets are not present, functionality which uses the secret will be disabled.

See [Safe storage of app secrets in development in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets) and [Azure Key Vault Configuration Provider in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration).

## Api:Key

The Api:Key secret is used to authenticate callers to certain API calls. 

If this key is missing, those API calls will fail.

## AdminUser

The keys AdminUser:Email and AdminUser:Password are used to deploy an administrative user, typically on first launch.

If this section is missing, such a user will not be created.

## AzureStorage

This section is used to construct a connection string to Azure Blob Storage. You\'ll need to set AzureStorage:AccountName and AzureStorage:AccountKey.

If this section is missing, blob storage features will not be available.
