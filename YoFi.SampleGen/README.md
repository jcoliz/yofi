# YoFi.SampleGen

This is an offline utility for generating realistic sample data. 
Originally, this was so users evaluating the app at www.try-yofi.com would be able to use the site effectively. 
Since then, I have found it to be a big help when creating new features and debugging old ones to have consistent data available.

The main idea is that we can define a "spending pattern" for a certain category. 
The generator will randomly create transactions to match that pattern across the year, according to the specified
variability (jitter) and frequency.

Directories here:

* [YoFi.SampleGen](./YoFi.SampleGen/): Class library containing the main logic of generation
* [YoFi.SampleGen.Tests](./YoFi.SampleGen.Tests): Unit tests for main logic
* [YoFi.SampleGen.Exe](./YoFi.SampleGen.Exe): Command line tool to generate the exact needed files

