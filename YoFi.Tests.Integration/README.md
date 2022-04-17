# Integration Tests for Asp.Net Application

These tests use the test server capability of ASP.NET Core to test the entire pipeline from web request
down through an in-memory database.

Here's the rules:

* "Given" clauses can set the database directly
* "When" clauses can only interact with the system using an HTTP request to an endpoint. Ideally just a single endpoint per test
* "Then" clauses can inspect the database, as well as inspect the response

Generally, no other interaction is allowed with the system under test.

Integration tests differ from functional tests in that:

* Functional tests follow a click path. Integration tests hit URLS directly
* Functional tests can only modify the database as a user would. Integration tests can do it directly.
* Functional tests can only inspect what the server responds. Integration tests can check the database.
* Functional tests use the final production database. Integration tests use an in-memory database.
* Functional tests use the final auth schemes. Integration tests work around auth.
* Functional tests can't use any objects of the system under test. Integration tests contain the main project, so can use any of its objects.
* Functional tests concern themselves with scenario coverage (i.e."user cans"). Integration tests strive for code coverage.