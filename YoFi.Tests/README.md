# Unit Tests

This directory contains the unit tests. They are organized thusly:

## Core

Directly tests items in the Core layer of the program, with a focus on testing as thinly and completely as possible.

## Controllers

Directly tests the controllers, again with a focus on testing as thinly and completely as possible.

## Database

Tests multiple different areas of the application, using an in-memory database. These could strictly be thought of as
integration tests, perhaps. The tests here are thicker, in that they go through multiple layers, and code coverage
is not as much of a big deal.

It's possible that this directory could go away in the future once I have proper integration tests that run
through the request/response pipeline, through application logic, all the way into the database.

## Common

Common Test Components. Application-independent componenets which may be useful in other similiar projects
as well.

## Helpers

Application dependent helper classes which provide functionality that can be use across many tests. 
Anything that takes a dependency on a YoFi namespace is a helper, as opposed to "Common" items which take no YoFi dependency.