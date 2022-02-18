# Unit Tests

This directory contains the unit tests. As opposed to the Integration Tests, the Unit Tests
are responsible for ensuring Core and Common code is thoroughly tested. Coverage for the AspNet
layer is the responsibility of the Integration Tests.

## Core

Directly tests items in the Core layer of the program, with a focus on testing as thinly and completely as possible.

## Controllers

Directly tests the controllers, again with a focus on testing as thinly and completely as possible.
This set of tests is somewhat dated, and may be removed in the future. Since the advent of high-coverage
Integration Tests, the unit tests are no longer concerned with the controllers, which is tested here.

## Common

Common Test Components. Application-independent componenets which may be useful in other similiar projects
as well.

## Helpers

Application dependent helper classes which provide functionality that can be use across many tests. 
Anything that takes a dependency on a YoFi namespace is a helper, as opposed to "Common" items which take no YoFi dependency.