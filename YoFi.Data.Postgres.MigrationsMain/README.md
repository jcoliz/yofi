# Migrations Main for Data.Postgres

In order to run migrations, EF Core needs a "main" app to run. To keep this simple,
we dedicate a whole app, with very minimal things going on, so we don't have to worry
about configuration, secrets, etc, which have nothing to do with the migration.

See: https://erwinstaal.nl/posts/db-per-tenant-catalog-database-ef-core-migrations/

## Make a migration

After making changes to the `ApplicationDbContext`, we need to add a migration
to describe how those changes will show up in the database. Migrations do need
to be added separately for Sql Server and Postgres.

```Powershell
dotnet ef migrations add $env:MIGRATION -o .\Migrations\ -n YoFi.Data.Postgres.Migrations --project .\YoFi.Data.Postgres\ --startup-project .\YoFi.Data.Postgres.MigrationsMain\ --context ApplicationDbContext
```

If you make a mistake and need to re-do it, be sure to remove the `ApplicationDbContextModelSnapshot.cs` file.
