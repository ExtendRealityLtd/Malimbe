# Changelog

# [5.2.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v5.1.0...v5.2.0) (2019-01-13)


### Features

* **XmlToTooltip:** allow customizing the XML tag replacement ([3da2ae3](https://github.com/ExtendRealityLtd/Malimbe/commit/3da2ae3))

# [5.1.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v5.0.0...v5.1.0) (2019-01-12)


### Bug Fixes

* **Runner:** don't cache the found configurations and weavers ([19c3533](https://github.com/ExtendRealityLtd/Malimbe/commit/19c3533))
* **SerializedProperty:** change field name in abstract classes ([a623ec2](https://github.com/ExtendRealityLtd/Malimbe/commit/a623ec2))
* **SerializedProperty:** match auto-property backing field exactly ([2f25dd4](https://github.com/ExtendRealityLtd/Malimbe/commit/2f25dd4))


### Features

* **XmlToTooltip:** annotate backing fields of properties ([0fcd7e9](https://github.com/ExtendRealityLtd/Malimbe/commit/0fcd7e9))

# [5.0.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v4.0.0...v5.0.0) (2019-01-12)


### Features

* **UnityIntegration:** don't implicitly reference any plugin .dll ([1e75894](https://github.com/ExtendRealityLtd/Malimbe/commit/1e75894))


### BREAKING CHANGES

* **UnityIntegration:** Referencing a Malimbe Editor assembly has to be
done explicitly from now on. Use Assembly Definition Files and their
inspector to reference the needed assemblies manually.

# [4.0.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v3.0.0...v4.0.0) (2019-01-11)


### Bug Fixes

* only cache types of needed assemblies ([693b0a0](https://github.com/ExtendRealityLtd/Malimbe/commit/693b0a0))
* **ClearProperty:** add support for generics in declaring type ([203df85](https://github.com/ExtendRealityLtd/Malimbe/commit/203df85))
* **ClearProperty:** don't inject into existing method ([b52819d](https://github.com/ExtendRealityLtd/Malimbe/commit/b52819d))
* **UnityIntegration:** compile all assemblies against netstandard2.0 ([20dcc67](https://github.com/ExtendRealityLtd/Malimbe/commit/20dcc67))
* **UnityIntegration:** reference runtime assembly in the editor one ([f801e5c](https://github.com/ExtendRealityLtd/Malimbe/commit/f801e5c))
* **ValidateProperties:** add support for generics in declaring type ([9137635](https://github.com/ExtendRealityLtd/Malimbe/commit/9137635))
* **ValidateProperties:** ensure to inject at end of existing method ([79f9f7c](https://github.com/ExtendRealityLtd/Malimbe/commit/79f9f7c))


### BREAKING CHANGES

* **ClearProperty:** The ClearPropertyMethod weaver no longer injects
anything into the Clear method in case it already exists. An info
logging statement has been added that can help to find the Clear
method implementations and ensure it clears the property manually.

# [3.0.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v2.0.0...v3.0.0) (2019-01-07)


### Code Refactoring

* **SerializedProperty:** invert the hidden-in-inspector setting ([2d04301](https://github.com/ExtendRealityLtd/Malimbe/commit/2d04301))


### BREAKING CHANGES

* **SerializedProperty:** The argument passed in the `SerializedProperty`
attribute constructor to hide the field in the inspector is now
negated. Uses need to be updated to pass the negation of what they
previously passed. The default value of the parameter has been
updated which means the default constructor call doesn't need to be
changed to upgrade to this change.

# [2.0.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v1.3.1...v2.0.0) (2019-01-07)


### Bug Fixes

* **Runner:** don't leak for already processed assemblies ([159e88a](https://github.com/ExtendRealityLtd/Malimbe/commit/159e88a))
* **Runner:** gracefully fail for unfound assemblies ([f0b60f9](https://github.com/ExtendRealityLtd/Malimbe/commit/f0b60f9))
* **Runner:** prevent access to disposed closure ([8ead72b](https://github.com/ExtendRealityLtd/Malimbe/commit/8ead72b))
* **SerializedProperty:** help serializing properties, not fields ([40b6511](https://github.com/ExtendRealityLtd/Malimbe/commit/40b6511))
* **UnityIntegration:** only weave each assembly once ([cbfd2e8](https://github.com/ExtendRealityLtd/Malimbe/commit/cbfd2e8))
* **UnityIntegration:** unnecessary creation of an intermediate list ([6d65c74](https://github.com/ExtendRealityLtd/Malimbe/commit/6d65c74))
* **UnityIntegration:** weave all assemblies on load ([d23bacb](https://github.com/ExtendRealityLtd/Malimbe/commit/d23bacb))


### Features

* **UnityIntegration:** allow weaving all assemblies manually ([b87448b](https://github.com/ExtendRealityLtd/Malimbe/commit/b87448b))
* **UnityIntegration:** show progress when weaving all assemblies ([2f1bb4c](https://github.com/ExtendRealityLtd/Malimbe/commit/2f1bb4c))


### BREAKING CHANGES

* **SerializedProperty:** Fields can no longer be "changed" into properties.
Instead of using a field and annotating it with
`FieldToPropertyAttribute` use a property and annotate it with
`SerializedPropertyAttribute`. Auto-implemented properties as well
as regular ones are supported and the only requirement is to have a
getter and setter (of any accessibility level).

## [1.3.1](https://github.com/ExtendRealityLtd/Malimbe/compare/v1.3.0...v1.3.1) (2019-01-06)


### Bug Fixes

* **UnityIntegration:** prevent loading assemblies ([1deef2f](https://github.com/ExtendRealityLtd/Malimbe/commit/1deef2f))

# [1.3.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v1.2.0...v1.3.0) (2019-01-05)


### Features

* **UnityIntegration:** rename assembly definition files ([3b24740](https://github.com/ExtendRealityLtd/Malimbe/commit/3b24740))

# [1.2.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v1.1.1...v1.2.0) (2019-01-05)


### Bug Fixes

* **UnityIntegration:** Unity doesn't pick up changes ([3205065](https://github.com/ExtendRealityLtd/Malimbe/commit/3205065))


### Features

* **Runner:** return whether the assembly was processed ([7222b22](https://github.com/ExtendRealityLtd/Malimbe/commit/7222b22))

## [1.1.1](https://github.com/ExtendRealityLtd/Malimbe/compare/v1.1.0...v1.1.1) (2019-01-05)


### Bug Fixes

* **release:** zip for GitHub release ([c9dae5e](https://github.com/ExtendRealityLtd/Malimbe/commit/c9dae5e))

# [1.1.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v1.0.6...v1.1.0) (2019-01-05)


### Features

* **release:** zip for GitHub release ([bc75269](https://github.com/ExtendRealityLtd/Malimbe/commit/bc75269))

## [1.0.6](https://github.com/ExtendRealityLtd/Malimbe/compare/v1.0.5...v1.0.6) (2018-12-23)


### Bug Fixes

* **UnityIntegration:** Unity .meta files for releases ([aa3b48a](https://github.com/ExtendRealityLtd/Malimbe/commit/aa3b48a))

## [1.0.5](https://github.com/ExtendRealityLtd/Malimbe/compare/v1.0.4...v1.0.5) (2018-12-17)


### Bug Fixes

* **packaging:** remove unused directory hint ([4f5a912](https://github.com/ExtendRealityLtd/Malimbe/commit/4f5a912))

## [1.0.4](https://github.com/ExtendRealityLtd/Malimbe/compare/v1.0.3...v1.0.4) (2018-12-17)


### Bug Fixes

* **packaging:** include sources without nesting the folders ([c2de9d8](https://github.com/ExtendRealityLtd/Malimbe/commit/c2de9d8))

## [1.0.3](https://github.com/ExtendRealityLtd/Malimbe/compare/v1.0.2...v1.0.3) (2018-12-17)


### Bug Fixes

* **UnityIntegration:** use Unity's package naming convention ([d160221](https://github.com/ExtendRealityLtd/Malimbe/commit/d160221))

## [1.0.2](https://github.com/ExtendRealityLtd/Malimbe/compare/v1.0.1...v1.0.2) (2018-12-15)


### Bug Fixes

* **Changelog:** changelog file location ([ef1bcee](https://github.com/ExtendRealityLtd/Malimbe/commit/ef1bcee))
* **License:** move third party notices into project root ([2094c45](https://github.com/ExtendRealityLtd/Malimbe/commit/2094c45))

## [1.0.1](https://github.com/ExtendRealityLtd/Malimbe/compare/v1.0.0...v1.0.1) (2018-12-15)


### Bug Fixes

* **UnityIntegration:** allow usage as a package outside the project ([c737c17](https://github.com/ExtendRealityLtd/Malimbe/commit/c737c17))

# 1.0.0 (2018-12-15)


### Bug Fixes

* **Weaver:** find Fody addin assemblies ([082f2c2](https://github.com/ExtendRealityLtd/Malimbe/commit/082f2c2))


### Features

* **Weaver:** only generate properties for tagged fields ([1542969](https://github.com/ExtendRealityLtd/Malimbe/commit/1542969))
