[![Malimbe logo][Malimbe-Image]](#)

> ### Malimbe
> A collection of tools to simplify writing public API components for the Unity software.

[![Release][Version-Release]][Releases]
[![License][License-Badge]][License]
[![Backlog][Backlog-Badge]][Backlog]

## Introduction

Malimbe for the [Unity] software aims to reduce repetitive boilerplate code by taking the assemblies that are created by build tools and changing the assembly itself, new functionality can be introduced and logic written as part of the source code can be altered. This process is called Intermediate Language (IL) weaving and Malimbe uses [Fody] to do it.

Malimbe helps running Fody and Fody addins without MSBuild or Visual Studio and additionally offers running them inside the Unity software by integrating with the Unity software compilation and build pipeline. Multiple weavers come with Malimbe to help with boilerplate one has to write when creating Unity software components that are intended for public consumption. This includes a form of "serialized properties", getting rid of duplicated documentation through XML documentation and the `[Tooltip]` attribute as well as weavers that help with ensuring the API is able to be called from `UnityEvent`s and more.

## Getting Started

### Adding the package to the Unity project manifest

* Navigate to the `Packages` directory of your project.
* Adjust the [project manifest file][Project-Manifest] `manifest.json` in a text editor.
  * Ensure `https://registry.npmjs.org/` is part of `scopedRegistries`.
    * Ensure `io.extendreality` is part of `scopes`.
  * Add `io.extendreality.malimbe` to `dependencies`, stating the latest version.

  A minimal example ends up looking like this. Please note that the version `X.Y.Z` stated here is to be replaced with [the latest released version][Latest-Release] which is currently [![Release][Version-Release]][Releases].
  ```json
  {
    "scopedRegistries": [
      {
        "name": "npmjs",
        "url": "https://registry.npmjs.org/",
        "scopes": [
          "io.extendreality"
        ]
      }
    ],
    "dependencies": {
      "io.extendreality.malimbe": "X.Y.Z",
      ...
    }
  }
  ```
* Switch back to the Unity software and wait for it to finish importing the added package.
* Anywhere in your Unity software project add a [`FodyWeavers.xml` file][FodyWeavers].
* Configure the various weavers Malimbe offers, e.g.:
    ```xml
    <?xml version="1.0" encoding="utf-8"?>

    <Weavers>
      <Malimbe.FodyRunner>
        <LogLevel>Error, Warning</LogLevel>
        <AssemblyNameRegex>^Zinnia</AssemblyNameRegex>
        <AssemblyNameRegex>^Assembly-CSharp</AssemblyNameRegex>
      </Malimbe.FodyRunner>
      <Malimbe.BehaviourStateRequirementMethod/>
      <Malimbe.MemberChangeMethod/>
      <Malimbe.MemberClearanceMethod/>
      <Malimbe.PropertySerializationAttribute/>
      <Malimbe.XmlDocumentationAttribute IdentifierReplacementFormat="`{0}`"/>
    </Weavers>
    ```
    As with any Fody weaver configuration the order of weavers is important in case a weaver should be applying to the previous weaver's changes.

    In case there are multiple configuration files all of them will be used. In that scenario, if multiple configuration files specify settings for the same weaver, a weaver will be configured using the values in the _last_ configuration file found. A warning is logged to notify of this behavior and to allow fixing potential issues that may arise by ensuring only a single configuration exists for any used weaver.

Additional weavers are supported. To allow Malimbe's Unity software integration to find the weavers' assemblies they have to be included anywhere in the Unity software project or in one of the UPM packages the project uses.

### Updating to the latest version

The package will show up in the Unity Package Manager UI once the above steps have been carried out. From then on the package can be updated by selecting the package in the Unity Package Manager and clicking on the `Update` button or using the version selection UI.

## Documentation

Check out the [Documentation] a further in-depth look at the features of Malimbe.

## Naming

Inspired by [Fody's naming] the name "Malimbe" comes from the [small birds][Malimbus] that belong to the weaver family [Ploceidae].

## Tools And Products Used

* [Fody]

## Contributing

Please refer to the Extend Reality [Contributing guidelines] and the [Unity project coding conventions].

## Code of Conduct

Please refer to the Extend Reality [Code of Conduct].

## License

Malimbe is released under the [MIT License][License].

Third-party notices can be found in [THIRD_PARTY_NOTICES.md][ThirdPartyNotices]

## Disclaimer

These materials are not sponsored by or affiliated with Unity Technologies or its affiliates. "Unity" and "Unity Package Manager" are trademarks or registered trademarks of Unity Technologies or its affiliates in the U.S. and elsewhere.

[Malimbe-Image]: https://user-images.githubusercontent.com/1029673/48707109-4d876080-ebf6-11e8-9476-4f084246771d.png
[Version-Release]: https://img.shields.io/github/release/ExtendRealityLtd/Malimbe.svg
[License-Badge]: https://img.shields.io/github/license/ExtendRealityLtd/Malimbe.svg
[Backlog-Badge]: https://img.shields.io/badge/project-backlog-78bdf2.svg

[Releases]: ../../releases
[License]: LICENSE.md
[Backlog]: http://tracker.vrtk.io

[Unity]: https://unity3d.com/
[Fody]: https://github.com/Fody/Fody
[Latest-Release]: ../../releases/latest
[FodyWeavers]: https://github.com/Fody/Fody#add-fodyweaversxml
[Documentation]: /Documentation/

[Fody's naming]: https://github.com/Fody/Fody#naming
[Malimbus]: https://en.wikipedia.org/wiki/Malimbus
[Ploceidae]: https://en.wikipedia.org/wiki/Ploceidae

[Contributing guidelines]: https://github.com/ExtendRealityLtd/.github/blob/master/CONTRIBUTING.md
[Unity project coding conventions]: https://github.com/ExtendRealityLtd/.github/blob/master/CONVENTIONS/UNITY3D.md
[Code of Conduct]: https://github.com/ExtendRealityLtd/.github/blob/master/CODE_OF_CONDUCT.md
[ThirdPartyNotices]: THIRD_PARTY_NOTICES.md