[![Malimbe logo][Malimbe-Image]](#)

> ### Malimbe
> A collection of tools to simplify writing public API components in Unity.

[![License][License-Badge]][License]
[![Waffle][Waffle-Badge]][Waffle]

## Introduction

Malimbe is a collection of tools to simplify writing public API components in Unity.

By taking the assemblies that are created by build tools and changing the assembly itself, repetetive boilerplate can be reduced, new functionality can be introduced and logic written as part of the source code can be altered. This process is called Intermediate Language (IL) weaving and Malimbe uses [Fody] to do it.

Malimbe helps running Fody and Fody addins without MSBuild or Visual Studio and additionally offers running them inside Unity by integrating with Unity's compilation and build pipeline. Multiple weavers come with Malimbe to help with boilerplate one has to write when creating Unity components that are intended for public consumption. This includes a form of "serialized properties", getting rid of duplicated documentation through XML documentation and the `[Tooltip]` attribute as well as weavers that help with ensuring the API is able to be called from `UnityEvent`s.

## Releases

| Branch | Version                                           | Explanation                        |
|--------|---------------------------------------------------|------------------------------------|
| latest | [![Release][Version-Release] ][Releases]          | Stable, production-ready           |
| next   | [![(Pre-)Release][Version-Prerelease] ][Releases] | Experimental, not production-ready |

Releases follow the [Semantic Versioning (SemVer) system][SemVer].

## Getting Started

Please follow these steps to install the package using a local location until Unity's Package Manager (UPM) allows third parties to publish packages to the UPM feed:

1. Download a release from the [Releases] page and extract it into your folder you use to keep your packages. It is recommended to make that folder part of your project and therefore [version controlled][VCS].
1. Open your Unity (`>= 2018.3`) project and follow [Unity's instructions][UPM-Instructions] on how to add the package to your project using UPM.
1. Anywhere in your Unity project add a [`FodyWeavers.xml` file][FodyWeavers].
1. Configure the various weavers Malimbe offers, e.g.:
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

Additional weavers are supported. To allow Malimbe's Unity integration to find the weavers' assemblies they have to be included anywhere in the Unity project or in one of the UPM packages the project uses.

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
  * There is no need to manually run the weaving process. The library just needs to be part of a Unity project (configured to only run in the Editor) to be used. It hooks into the various callbacks Unity offers and automatically weaves any assembly on startup as well as when they change.
  * Once the library is loaded in the Editor a menu item `Tools/Malimbe/Weave All Assemblies` allows to manually trigger the weaving process for all assemblies in the current project. This is useful when a `FodyWeavers.xml` file was changed.
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
  * If necessary the method and the base type's method will be adjusted to override the method of the same name. Accessibility levels are also adjusted as needed.
* `XmlDocumentationToFieldTooltip.Fody`: A generic weaver (though made for Unity). Looks up the XML `<summary>` documentation for any field that is public or uses `[SerializeField]` and ensures `[Tooltip]` is used on that field with that summary.
  * The weaver only runs on types that match a namespace. Specify the namespaces to act on via (multiple) XML _elements_ called `NamespaceFilter`. The elements' values are used as ([.NET Standard's][Regex]) regular expressions.
  * Instead of `TooltipAttribute` the attribute can be customized with the XML _attribute_ `FullAttributeName`, e.g.:
    ```xml
      <Malimbe.XmlDocumentationToFieldTooltip FullAttributeName="Some.Namespace.DocumentationAttribute">
        <NamespaceFilter>^VRTK</NamespaceFilter>
      </Malimbe.XmlDocumentationToFieldTooltip>
    ```
    The attribute needs to have a constructor that takes a `string` parameter and nothing else. Note that the attribute name has to be the full type name, i.e. prefixed by the namespace.
  * In case the attribute already exists on the field it will be replaced.
* `UnityPackaging`: Outputs a ready-to-use folder with the appropriate hierarchy to copy into a Unity project's Asset folder. The output includes both the Unity integration libraries as well as all weavers listed above.

## Contributing

If you want to raise a bug report or feature request please follow [SUPPORT.md][Support].

While we intend to add more features to Malimbe when we identify a need or use case, we're always open to take contributions! Please follow the contribution guidelines found in [CONTRIBUTING.md][Contributing].

## Naming

Inspired by [Fody's naming] the name "Malimbe" comes from the [small birds][Malimbus] that belong to the weaver family [Ploceidae].

## Tools and Products Used

 * [Fody]

## License

Malimbe is released under the [MIT License][License].

Third-party notices can be found in [THIRD_PARTY_NOTICES.md][ThirdPartyNotices]

[Malimbe-Image]: https://user-images.githubusercontent.com/1029673/48707109-4d876080-ebf6-11e8-9476-4f084246771d.png
[License-Badge]: https://img.shields.io/github/license/ExtendRealityLtd/Malimbe.svg
[Waffle-Badge]: https://badge.waffle.io/ExtendRealityLtd/Malimbe.svg?columns=Bug%20Backlog,Feature%20Backlog,In%20Progress,In%20Review
[Version-Release]: https://img.shields.io/github/release/ExtendRealityLtd/Malimbe.svg
[Version-Prerelease]: https://img.shields.io/github/release-pre/ExtendRealityLtd/Malimbe.svg?label=pre-release&colorB=orange

[Waffle]: https://waffle.io/ExtendRealityLtd/Malimbe
[Releases]: ../../releases
[CD]: https://dev.azure.com/ExtendReality/VRTK/_build/latest?definitionId=2
[SemVer]: https://semver.org/
[VCS]: https://en.wikipedia.org/wiki/Version_control
[UPM-Instructions]: https://docs.unity3d.com/Packages/com.unity.package-manager-ui@2.1/manual/index.html#extpkg
[SemVer-Build]: https://semver.org/#spec-item-10
[FodyWeavers]: https://github.com/Fody/Fody#add-fodyweaversxml
[Regex]: https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions

[Fody's naming]: https://github.com/Fody/Fody#naming
[Malimbus]: https://en.wikipedia.org/wiki/Malimbus
[Ploceidae]: https://en.wikipedia.org/wiki/Ploceidae
[Fody]: https://github.com/Fody/Fody

[Support]: /.github/SUPPORT.md
[Contributing]: /.github/CONTRIBUTING.md
[License]: LICENSE.md
[ThirdPartyNotices]: THIRD_PARTY_NOTICES.md
