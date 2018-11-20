[![image](https://user-images.githubusercontent.com/1029673/48707109-4d876080-ebf6-11e8-9476-4f084246771d.png)](README.md)

> ### Malimbe
> A collection of tools to simplify writing public API components in Unity.

[![Waffle](https://img.shields.io/badge/project-backlog-78bdf2.svg)][Waffle]

## Introduction

Malimbe is a collection of tools to simplify writing public API components in Unity.

## Getting Started

* Download or clone this repository.
* Build the solution in Visual Studio or via MSBuild (in `Release` configuration).
* The `UnityPackaging` project's output folder contains all the files necessary in a Unity project. Copy the output into your project's `Assets` folder.
* Anywhere in your Unity project [add a `FodyWeavers.xml` file][FodyWeavers].
* Configure the various weavers Malimbe offers, e.g.:
  ```xml
  <?xml version="1.0" encoding="utf-8"?>

  <Weavers>
      <Malimbe.FodyRunner>
        <LogLevel>Error</LogLevel>
      </Malimbe.FodyRunner>
      <Malimbe.FieldToProperty/>
      <Malimbe.ClearPropertyMethod>
        <NamespaceFilter>^VRTK</NamespaceFilter>
      </Malimbe.ClearPropertyMethod>
      <Malimbe.ValidatePropertiesMethod>
        <NamespaceFilter>^VRTK</NamespaceFilter>
      </Malimbe.ValidatePropertiesMethod>
      <Malimbe.XmlDocumentationToFieldTooltip>
        <NamespaceFilter>^VRTK</NamespaceFilter>
      </Malimbe.XmlDocumentationToFieldTooltip>
  </Weavers>
  ```
  As with any Fody weaver configuration the order of weavers is important in case a weaver should be applying to the previous weaver's changes.

## What's in the Box

Malimbe is a _collection_ of tools. Each project represents a solution to a specific issue:

* `FodyRunner`: A standalone library that allows running Fody without MSBuild or Visual Studio.
  * Use the XML element `LogLevel` to specify which log messages should be sent to the logger instance. Valid values are

    * `None` (or don't specify `LogLevel`)
    * `Debug`
    * `Info`
    * `Warning`
    * `Error`
    * `All`

    Separate multiple levels by using multiple XML elements or separate inside an XML element by using any form of whitespace including newlines or commas.
* `FodyRunner.UnityIntegration`: Weaves assemblies in the Unity Editor after Unity compiled them as well as builds. The weaving is done by utilizing `FodyRunner`.
  * The library just needs to be part of a Unity project (configured to only run in the Editor) to be used. It hooks into the various callbacks Unity offers.
* `FieldToProperty.Fody`: A Unity-specific weaver. Creates a property for all fields annotated with `[BacksProperty]`. If a `T SetFieldName(T, T)` method exists it will be called in the property's setter. Adds `[SerializeField]` to the field if not yet specified.
  * Annotate a field with `[BacksProperty]` to use this.
  * Optionally write `T SetFieldName(T, T)` methods that act as a setter replacement on the same type that declares the field (of type `T`). The accessibility level of the method doesn't matter and the name lookup is case insensitive.
* `ClearPropertyMethod.Fody`: A generic weaver. Creates `ClearProperty()` methods for any property that is of reference type and has a setter. Sets the property via its setter to `null` in this new method.
  * The weaver only runs on types that match a namespace. Specify the namespaces to act on via (multiple) XML _elements_ called `NamespaceFilter`. The elements' values are used as ([.NET Standard's][Regex]) regular expressions.
  * In case the method already exists the additional instructions will be weaved into the _end_ of the method. The method name lookup is case insensitive.
* `ValidatePropertiesMethod.Fody`: A generic weaver (though made for Unity). Ensures there's an `public OnValidate()` method for any type that has properties with setters. For each property it does `Property = Property;` in this new method.
  * The weaver only runs on types that match a namespace. Specify the namespaces to act on via (multiple) XML _elements_ called `NamespaceFilter`. The elements' values are used as ([.NET Standard's][Regex]) regular expressions.
  * Instead of `OnValidate` the method name can be customized with the XML _attribute_ `MethodName`, e.g.:
    ```xml
      <Malimbe.ValidatePropertiesMethod MethodName="Validate">
        <NamespaceFilter>^VRTK</NamespaceFilter>
      </Malimbe.ValidatePropertiesMethod>
    ```
  * In case the method already exists the additional instructions will be weaved into the _end_ of the method. The method name lookup is case insensitive.
  * If necessary the method and will be adjusted to override a base type's method of the same name, Accessibility levels are also adjusted as needed.
* `XmlDocumentationToFieldTooltip.Fody`: A generic weaver (though made for Unity). Looks up the XML `<summary>` documentation for any field that is public or uses `[SerializeField]` and ensures `[Tooltip]` is used on that field with that summary.
  * The weaver only runs on types that match a namespace. Specify the namespaces to act on via (multiple) XML _elements_ called `NamespaceFilter`. The elements' values are used as ([.NET Standard's][Regex]) regular expressions.
  * Instead of `TooltipAttribute` the attribute can be customized with the XML _attribute_ `FullAttributeName`, e.g.:
    ```xml
      <Malimbe.XmlDocumentationToFieldTooltip FullAttributeName="Some.Namespace.DocumentationAttribute">
        <NamespaceFilter>^VRTK</NamespaceFilter>
      </Malimbe.XmlDocumentationToFieldTooltip>
    ```
    The attribute needs to have a constructor takes a `string` parameter and nothing else. Note that the attribute name has to be the full type name, i.e. prefixed by the namespace.
  * In case the attribute already exists on the field it will be replaced.
* `UnityPackaging`: Outputs a ready-to-use folder with the appropriate hierarchy to copy into a Unity project's Asset folder. The output includes both the Unity integration libraries as well as all weavers listed above.

## Contributing

We would love to get contributions from you! Please follow the contribution guidelines found in [CONTRIBUTING.md][Contributing].

## Naming

Inspired by [Fody's naming] the name "Malimbe" comes from the [small birds][Malimbus] that belong to the weaver family [Ploceidae].

## Tools and Products Used

 * [Fody]

## License

Code released under the [MIT License][License].

[Waffle]: https://waffle.io/ExtendRealityLtd/Malimbe
[FodyWeavers]: https://github.com/Fody/Fody#add-fodyweaversxml
[Regex]: https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions

[Fody's naming]: https://github.com/Fody/Fody#naming
[Malimbus]: https://en.wikipedia.org/wiki/Malimbus
[Ploceidae]: https://en.wikipedia.org/wiki/Ploceidae
[Fody]: https://github.com/Fody/Fody

[Contributing]: /.github/CONTRIBUTING.md
[License]: LICENSE.md
