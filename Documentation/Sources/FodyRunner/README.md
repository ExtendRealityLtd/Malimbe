# `FodyRunner`

A standalone library that allows running Fody without MSBuild or Visual Studio.

* Use the XML _element_ `LogLevel` to specify which log messages should be sent to the logger instance. Separate multiple levels by using multiple XML elements or separate inside an XML element by using any form of whitespace including newlines or commas. Valid values are
  * `None` (or don't specify `LogLevel`)
  * `Debug`
  * `Info`
  * `Warning`
  * `Error`
  * `All`
* Add XML _elements_ `AssemblyNameRegex` for each assembly that should be processed. Specifying none will result in no assembly being processed and a warning being logged. The elements' values are used as ([.NET Standard's][Regex]) regular expressions.

[Regex]: https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions