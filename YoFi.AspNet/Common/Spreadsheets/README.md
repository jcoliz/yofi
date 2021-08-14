# Spreadsheet helpers
My goal is to isolate all spreadsheet reading/writing into ONE class
or namespace, so this is the ONLY place that the underlying
dependant library is touched.

## Scenarios

1. Given an open stream to a Spreadsheet, find a single named sheet, read the rows into an IEnumerable<T>
2. Same as #1, plus there can optionally be a second named sheet, whos rows are read into a different IEnumerable<T>
3. Given an open result stream, and an IEnumerable<T>, create a new spreadsheet, then a new named sheet, write the objects to it as rows.
4. Same as #3, plus there can be a second IEnumerable<T>, which is written into a second sheet.

I'm separating these into an interface to ensure that when I refactor
spreadsheet handling, I present the same interface externally.