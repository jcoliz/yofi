# YoFi.SampleGen

This is a class library containing the main logic of generation.

* [SampleDataPattern](./SampleDataPattern.cs): The main idea is that we can define group of "spending patterns" each for a certain category. Pattern objects also contain the logic to randomly generate transactions meeting the pattern.
* [SampleDataGenerator](./SampleDataGenerator.cs): The generator handles getting the generated data into a form that matches the YoFi data model. The generator can handle a single set of definitions, and output it in a single specified form.
* [SampleDataRunner](./SampleDataRunner.cs): The runner controls loading named set of patterns, then output files in various configurations to various directories, as specified the the configuration json file. That config file can simple be deserialized in as a SampleDataRunner. The runner can handle many different sets of definitions, and save them into many different forms.