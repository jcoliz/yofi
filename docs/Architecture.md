# Architecture

## Summary

I'm working to more strictly separate code into conceptually distinct layers. In the future, I hope to 
increase the separation futher by putting each layer into its own project.

* AspNet: Handles HTTP receive/respond. 
* Data: Handles SQL Server interaction.
* Core: Application logic
* Common: Components that multiple apps could use to provide their application logic
* Platform: Underlying technology

## AspNet

The rule here is that this is the only layer that can use anything in the "Microsoft.AspNetCore" namespace. 
Furthermore, I try to only do things in this layer which require that namespace. As soon as it can be done in
another layer, I try to move it to the other layer.

Controllers, views, and pages all live here. If controllers or pages need to DO something, they're expected
to call into a Core class, like a Repository, to do it.

## Data

The rule here is that this is the only layer which can use anything in Entity Framework.
Implementing this will take some more work, as EFCore interaction is spread around quite a bit.
The vision is that this layer implements the IDataContext interface, and then everyone
else uses it to access the data store.

## Core

This is the application logic. If the app were to be ported to, let's say, a native app with on-disk storage
(e.g. SQLite), most or all of this code would continue to be used.

## Common

Same concept as Core, but items in the Common layer are general-purpose enough to share with multiple apps.

## Platform

I'm still working on clear definition for what goes in this layer, and how does Core interact with it.
For example, should Ofx file and spreadsheet parsing be confined to Platform classes? Currently,
I have Core just directly take the dependency on those items.