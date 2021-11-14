# Reports Architecture

## IReportEngine

The interface defining what the UI will know about reports.

## ReportBuilder

Builds application-specific reports. This is the top-level entry point for building reports.
It is where the app logic lives to arrange the data we have in our
database into reports that will be interesting for the user.

Mainly ReportBuilder is concerned with creating a Report based on what's specified
in the ReportDefintion, using QueryBuilder to do the heavy lifting.

- Implements IReportEngine
- Contains a QueryBuilder
- Contains ReportDefinitions
- Produces a Report
- Consumes an IDataContext (passes it immediately to QueryBuilder)

## ReportParameters

Parameters used to build a report at one moment in time. Data only class

- Refers to ReportDefinition by ID

## ReportDefinition

Defines a single kind of report. Contains data only, no logic.

The idea is to remove report defintion from code, into data. This object
is something we can later store in the database.

## QueryBuilder

Builds EF Core queries for app-specific scenarios.
This class has the unenviable task of building EF queries for
the various scenarios we might want to report about, specific
to this application's data.
 
The challenge is building queries that EF can translate later on
when month/category groupings are built. Sometimes it can't be done
in which case we'll turn it into a client-side query here.
 
Note that all public methods return an IEnumerable(NamedQuery) because
this is what the Report class expects as a source, even if there is only
a single query in it.
 
Architecturally, this class is a challenge. It absolutely represents
busines logic, and needs to know all about the conceptual structure
of our data. At the same time, it is tightly bound to the abilities
of EF Core to generate MS SQL Server queries.
 
So, while this class does consume an IDataContext, and therefore has
interface separation, if we were to provide an IDataContext backed
by some other store, it's highly likely that this class would need
some work.

- Consumes an IDataContext
- Consumes model items by name (from IDataContext)
- Consumes a ReportDefinition 
	(could definitely be reduced, it only uses Source & SourceParameters, perhaps into IReportSourceDefinition)
- Produces NamedQueries

## NamedQuery

Is an IQueryable(IReportable) with a string name. Data-only class with 
some small amount of logic to create derivative NamedQuery objects from
existing ones.

## Report

Table-based report: Arranges IReportable items into tables

This is where the queries actually get called. However, nothing here directly calls
into the IDataContext, because the the NamedQuery contains IQueryables which were
given to use by the IDataContext in QueryBuilder. This will cause issues later,
though, when we try to move report generation to be async by calling SumAsync
in BuildPhase_Group.

General usage: 
1. Set the Source
2. Set any needed configuration properties
3. Build the report
4. Iterate over rows/columns to display the report

Note that there is no app-specific logic of any form in this
class.

- Implements IDisplayReport
- Consumes NamedQuery
- Consumes IReportable items (contained in a NamedQuery)
- Consumes a ReportDefinition
- Contains a Table

## ManualReport

A report you create yourself by filling in the table

Because it implements IDisplayReport, it can be displayed the same was as a
fully-created report.

- Implements IDisplayReport
- Contains a Table

## IDisplayReport

The interface used by the report renderer to display a report in HTML

## Table

Generic dictionary of (TColumn,TRow) to TValue. Low-level building block which store the final arranged result of report data.

## IReportable

Defines a single item which can be included in the aggregation done by a report

## Pages/Report(s).cshtml

Provides user interaction with reports

- Consumes IReportEngine

## Views/Shared/DisplayReport.cshtml

Partial view, displays an IDisplayReport nicely on an HTML page.

- Consumes an IDisplayReport

##  YoFi.AspNet.Data.ApplicationDbContext

Contains the data for the application using EF Core

- Implements IDataContext
- Stores model items which implement IReportable