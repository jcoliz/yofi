# Integration tests

The tests in this directory are truly 3-level tests, in that they enter through the controllers,
test app logic in the core with repositories and reports, and run real-life EF Core queries in
the data layer.

What they don't test is the HTTP request/response pipeline and the actual on-disk SQL server database.

The next step for tests in this directory is to move to a Test Server, and test them via HTML 
request/response. ReportBuilderTest is the place to start.