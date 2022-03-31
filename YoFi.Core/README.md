# YoFi Core

Business logic and models, independent of any storage or UI framework.

The vision for this directory is that ultimately I'll pull it out into its own project
as a step for architectural separation.

There should be no direct database interaction. Instead, all database interaction goes
through IDataContext interface, which ApplicationDbContext implements.

As a future step, many tests can be rewritten to remove EF Core from them. Ultimately
perhaps EFCore will only be used for integration tests.

My current plan is to move any/all logic from controllers into here, as a step to
put the controllers on a diet.

It would be a cool proof of concept to make a console app version of YoFi which]
exercised everything in here and use SQlite.