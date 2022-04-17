# YoFi Asp.Net Application

[![System Architecture](/docs/images/YoFi-System-Architecture.svg)](https://raw.githubusercontent.com/jcoliz/yofi/master/docs/images/YoFi-System-Architecture.svg)

## Common

Application-independent code which may be reused across multiple applications. Many of these are in the YoFi.Core project. 
I may someday make a "Shared Kernel" project.

## Controllers

ASP.NET Controllers, matching with the views

## Core

Business logic and models, independent of any storage or UI framework. This is now separated out into the YoFi.Core project.

## Data

Entity Framework Core database definitions. This is now separated out into the YoFi.Data project.

## Main

Program.Main and Startup, application initializers

## Pages

ASP.NET Pages

## Services

Definitions to access cloud services other than databases. These will in the future get separated into either a YoFi.Services project or individual YoFi.Services.{ServiceName} projects.
The driving force for this change will be when the Vue.Js application gets far enough along to use these services.

## Views

ASP.NET Views
