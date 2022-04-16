# YoFi.SampleGen

This is a class library containing the main logic of generation.

* [SampleDataPattern](./SampleDataPattern.cs): The main idea is that we can define group of "spending patterns" each for a certain category. 
* [SampleDataGenerator](./SampleDataGenerator.cs): The generator will randomly create transactions to match that pattern across the year, according to the specified variability (jitter) and frequency.
* [SampleDataRunner](./SampleDataRunner.cs): The runner controls loading named set of patterns, then output files in various configurations to various directories, as specified the the configuration json file. That config file can simple be deserialized in as a SampleDataRunner.