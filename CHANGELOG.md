# Changelog

### [9.6.5](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.6.4...v9.6.5) (2019-12-21)

#### Bug Fixes

* **MemberChange:** refactor inspector logic to be more composable ([d82413c](https://github.com/ExtendRealityLtd/Malimbe/commit/d82413cdf0eea286a20642128abd3e1a43ef9a14))
  > The previous refactor of the custom unity InspectorEditor did not split out enough of the logic into their own methods so they were not completely usable independently.
  > 
  > This refactor splits out the methods even more so they are more independent and can be easily used on their own or overridden.

### [9.6.4](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.6.3...v9.6.4) (2019-12-21)

#### Bug Fixes

* **deps:** use latest pipeline templates ([8269efb](https://github.com/ExtendRealityLtd/Malimbe/commit/8269efbdc4772447c11060638466c4363bb21d1c))
  > There is an issue with the previous template not correctly building the Unity software image. This latest version should fix the issue.
* **MemberChange:** refactor custom inspector logic to be composable ([17abe41](https://github.com/ExtendRealityLtd/Malimbe/commit/17abe41968d2da9e443a42469153934aa5879aef))
  > The custom unity InspectorEditor has now been refactored so the logic within the OnInspectorGUI has been separated out into more logical chunks so these different methods can be called separately or overriden where required.
  > 
  > The functionality of the inspector has not changed in anyway so this fix is purely just making method logic more accessible.

### [9.6.3](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.6.2...v9.6.3) (2019-12-02)

#### Bug Fixes

* **MemberChange:** prevent Before/AfterChange being called at edit time ([7d6077b](https://github.com/ExtendRealityLtd/Malimbe/commit/7d6077b4b6fdb346fdf571786f3a185b20ab88f8)), closes [/github.com/ExtendRealityLtd/Malimbe/blob/master/Sources/FodyRunner.UnityIntegration/InspectorEditor.cs#L85-L88](https://github.com//github.com/ExtendRealityLtd/Malimbe/blob/master/Sources/FodyRunner.UnityIntegration/InspectorEditor.cs/issues/L85-L88)
  > There was a previous fix (https://github.com/ExtendRealityLtd/Malimbe/commit/40baf008d804d34f00ffea6aac5c02fbd9362ef0) that attempted to fix the following issue: (This is being described in detail as the message on the other commit is unhelpful).
  > 
  > The Malimbe custom Unity InspectorEditor would only run the `BeforeChange` and `AfterChange` methods when valid ChangeHandler attributes were found in the component (e.g. `OnBeforeChange()` and `OnAfterChange()`. However, when using a Zinnia ObservableList it would not raise the component events when the Elements array was updated in the inspector.
  > 
  > This is due to the Zinnia ObservableList using a custom inspector (`ObservableListEditor`) which extends the Malimbe InspectorEditor and overrides the `BeforeChange()` and `AfterChange()` methods to raise events when the list elements have items added/removed from them.
  > 
  > The problem rose from the ObservableList component does not contain any ChangeHandler attributes and therefore the `ChangeHandlerMethodInfos` would be empty and so the check in the `OnInspectorGUI()` method

### [9.6.2](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.6.1...v9.6.2) (2019-11-27)

#### Bug Fixes

* **RequiredBehaviourState:** don't use isActiveAndEnabled ([8d73d3d](https://github.com/ExtendRealityLtd/Malimbe/commit/8d73d3d0f6c1ee22a7987a4591e5000caa6fa80b))
  > Unity contains a bug wherein `isActiveAndEnabled` is `false` even though both `enabled` and `gameObject.activeSelf` are `true`.
  > 
  > `isActiveAndEnabled` is largely a convenience and by checking `enabled` and `gameObject.activeInHierarchy` this issue is avoided while remaining functionally identical.

### [9.6.1](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.6.0...v9.6.1) (2019-10-28)

#### Bug Fixes

* **README.md:** provide more concise release data and update info ([9e87ea2](https://github.com/ExtendRealityLtd/Malimbe/commit/9e87ea295f6de7c03029a2e31de78fcdd9b29353))
  > The Releases section has been removed and is now just a simple badge at the top of the README. There has been an additional section in `Getting Started` on how to update the package via the Unity Package Manager.
  > 
  > The links have also been ordered in the order of appearance in the document.

## [9.6.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.5.3...v9.6.0) (2019-10-26)

#### Features

* **.github:** use organization .github repository ([a3ea4ae](https://github.com/ExtendRealityLtd/Malimbe/commit/a3ea4aead478821cd64158ad5b16920f018c3686))
  > GitHub provides a mechanism where a global organization .github repo can be used as a fallback to provide default community health files instead of repeating the same files across multiple repos.
  > 
  > ExtendRealityLtd now has a `.github` repo which should be used as it provides the correct details for this repo.
  > 
  > The README.md has been updated to provide definitive links to the relevant files.

#### Miscellaneous Chores

* add dependabot configuration ([d454af5](https://github.com/ExtendRealityLtd/Malimbe/commit/d454af5e8d8266aeb7d31520f09522e99fd376a3))

### [9.5.3](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.5.2...v9.5.3) (2019-10-20)

#### Documentation

* **CONTRIBUTING:** do not include copyright notices ([44dd7bc](https://github.com/ExtendRealityLtd/Malimbe/commit/44dd7bc8938aaf7dd8f50c0b55c818996d7b8886)), closes [/help.github.com/en/articles/github-terms-of-service#6](https://github.com//help.github.com/en/articles/github-terms-of-service/issues/6)
  > Authors will continue to retain the copyright for the code committed but do so under the license stated in the repository as outlined in the [GitHub Terms Of

#### Miscellaneous Chores

* **deps:** use latest pipeline templates ([e6c200d](https://github.com/ExtendRealityLtd/Malimbe/commit/e6c200d30121d663e3c6ce9f4dae1986ce7af625))

### [9.5.2](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.5.1...v9.5.2) (2019-10-15)

#### Bug Fixes

* **Runner:** duplicated configuration files result in warnings ([727c8ad](https://github.com/ExtendRealityLtd/Malimbe/commit/727c8ad9d124de94f8afa2cbbe8bb3da1a4c60b9))
  > The fix is to just make sure the found configuration files are distinct.

### [9.5.1](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.5.0...v9.5.1) (2019-10-14)

#### Bug Fixes

* **ci:** back to npmjs.com ([563988f](https://github.com/ExtendRealityLtd/Malimbe/commit/563988f4fe5ade6274de7886607563f71c4a8180))
  > GitHub's npm feeds only allow publishing scoped packages, but UPM doesn't support those.

## [9.5.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.4.4...v9.5.0) (2019-10-14)

#### Features

* **ci:** publish package on GitHub feed ([9c5021e](https://github.com/ExtendRealityLtd/Malimbe/commit/9c5021e07d6860b42ca7e48eaf12c8dc6d9c41b6))

#### Bug Fixes

* **ci:** use latest CD template ([2d10e3a](https://github.com/ExtendRealityLtd/Malimbe/commit/2d10e3a7515aeddb966e10c3c2255a3d67b075c2))

#### Continuous Integration

* use latest DevOps templates ([9706a1d](https://github.com/ExtendRealityLtd/Malimbe/commit/9706a1d987c91f327fe8f9fd0c780ed88539b05c))
* use latest DevOps templates ([dae96d9](https://github.com/ExtendRealityLtd/Malimbe/commit/dae96d9d9211efbcf7e626e62809fb1429c59256))

#### Miscellaneous Chores

* **devops:** use latest DevOps templates ([fa556c6](https://github.com/ExtendRealityLtd/Malimbe/commit/fa556c62494d5490fd92979d7c609220cec5af02))
* **packaging:** remove unused package manifest field ([d6e4ecb](https://github.com/ExtendRealityLtd/Malimbe/commit/d6e4ecbbc2f756f8a9421cf12602b439b44484f9))

## [9.4.4](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.4.3...v9.4.4) (2019-10-13)


### Bug Fixes

* **packaging:** Unity editor assemblies path ([35b1cb2](https://github.com/ExtendRealityLtd/Malimbe/commit/35b1cb2f00e1f901d958e01d053b3b7e226e2aa7))

## [9.4.3](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.4.2...v9.4.3) (2019-06-16)


### Bug Fixes

* **MemberChange:** allow subclasses to handle property changes ([40baf00](https://github.com/ExtendRealityLtd/Malimbe/commit/40baf00))

## [9.4.2](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.4.1...v9.4.2) (2019-03-25)


### Bug Fixes

* **MemberChange:** only show undo/redo warning for change handlers ([e4cbe10](https://github.com/ExtendRealityLtd/Malimbe/commit/e4cbe10))

## [9.4.1](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.4.0...v9.4.1) (2019-03-23)


### Bug Fixes

* **MemberChange:** don't reset changes to other fields in handlers ([56e9357](https://github.com/ExtendRealityLtd/Malimbe/commit/56e9357)), closes [#39](https://github.com/ExtendRealityLtd/Malimbe/issues/39)
* **MemberChange:** fix referencing fields from base types ([91a0355](https://github.com/ExtendRealityLtd/Malimbe/commit/91a0355))
* **MemberChange:** only look up change handlers once ([beb5507](https://github.com/ExtendRealityLtd/Malimbe/commit/beb5507))

# [9.4.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.3.1...v9.4.0) (2019-03-21)


### Bug Fixes

* **MemberChange:** exception thrown when using multiple attributes ([d227180](https://github.com/ExtendRealityLtd/Malimbe/commit/d227180))
* **MemberChange:** only show undo/redo warning at runtime ([41661a5](https://github.com/ExtendRealityLtd/Malimbe/commit/41661a5))


### Features

* **MemberChange:** allow handling changes to superclass member ([33c2c2a](https://github.com/ExtendRealityLtd/Malimbe/commit/33c2c2a))

## [9.3.1](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.3.0...v9.3.1) (2019-03-20)


### Bug Fixes

* **MemberChange:** allow undo operations but warn against using them ([b29c017](https://github.com/ExtendRealityLtd/Malimbe/commit/b29c017))
* **MemberChange:** don't do change handler checks multiple times ([311643d](https://github.com/ExtendRealityLtd/Malimbe/commit/311643d))
* **MemberChange:** support multiple field store instructions ([7e504ff](https://github.com/ExtendRealityLtd/Malimbe/commit/7e504ff))

# [9.3.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.2.2...v9.3.0) (2019-03-16)


### Features

* **MemberChange:** help preventing infinite loops ([d490ded](https://github.com/ExtendRealityLtd/Malimbe/commit/d490ded))

## [9.2.2](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.2.1...v9.2.2) (2019-03-12)


### Bug Fixes

* **packaging:** use latest Unity version ([e8fa0d5](https://github.com/ExtendRealityLtd/Malimbe/commit/e8fa0d5))
* **UnityIntegration:** improve performance by deferring lookups ([b83d158](https://github.com/ExtendRealityLtd/Malimbe/commit/b83d158))

# [9.2.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.1.2...v9.2.0) (2019-03-12)


### Features

* **UnityIntegration:** allow customizing InspectorEditor ([fc33861](https://github.com/ExtendRealityLtd/Malimbe/commit/fc33861))

## [9.1.2](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.1.1...v9.1.2) (2019-03-09)


### Bug Fixes

* **UnityIntegration:** only call change handlers for changed member ([ed3f93f](https://github.com/ExtendRealityLtd/Malimbe/commit/ed3f93f))
* **XmlToTooltip:** single XML tag in single summary ([b066080](https://github.com/ExtendRealityLtd/Malimbe/commit/b066080))

## [9.1.1](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.1.0...v9.1.1) (2019-03-06)


### Bug Fixes

* **MemberChange:** support generic types ([d93148c](https://github.com/ExtendRealityLtd/Malimbe/commit/d93148c))

# [9.1.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v9.0.0...v9.1.0) (2019-03-04)


### Bug Fixes

* **UnityIntegration:** exception when unselecting scripts in project ([dfc8628](https://github.com/ExtendRealityLtd/Malimbe/commit/dfc8628))


### Features

* **MemberChange:** only call change methods when isActiveAndEnabled ([d0b0120](https://github.com/ExtendRealityLtd/Malimbe/commit/d0b0120))

# [9.0.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v8.2.4...v9.0.0) (2019-03-04)


### Bug Fixes

* **PropertySerialization:** fix null reference exception ([97fdf72](https://github.com/ExtendRealityLtd/Malimbe/commit/97fdf72))
* **UnityIntegration:** remove manual weaving menu item entry ([1404254](https://github.com/ExtendRealityLtd/Malimbe/commit/1404254))


### Code Refactoring

* **PropertyValidation:** remove property validation ([1e1c8f7](https://github.com/ExtendRealityLtd/Malimbe/commit/1e1c8f7))


### Features

* **MemberChange:** overhaul PropertySetter for Unity usage ([173a358](https://github.com/ExtendRealityLtd/Malimbe/commit/173a358))


### BREAKING CHANGES

* **MemberChange:** `CalledBySetterAttribute` has been replaced with
`CalledBeforeChangeOfAttribute` and `CalledAfterChangeOfAttribute`.
Please see the latest Readme for more details.
* **PropertyValidation:** Property validation has been removed from Malimbe.
For an alternative solution please look out for a future version of
this project or
[Zinnia](https://github.com/ExtendRealityLtd/Zinnia.Unity).

## [8.2.4](https://github.com/ExtendRealityLtd/Malimbe/compare/v8.2.3...v8.2.4) (2019-02-27)


### Bug Fixes

* **PropertyValidation:** import generic arguments ([ca0fcd3](https://github.com/ExtendRealityLtd/Malimbe/commit/ca0fcd3))

## [8.2.3](https://github.com/ExtendRealityLtd/Malimbe/compare/v8.2.2...v8.2.3) (2019-02-24)


### Bug Fixes

* **Runner:** prevent exception when no config is used ([164877f](https://github.com/ExtendRealityLtd/Malimbe/commit/164877f))
* **UnityIntegration:** prevent NullReferenceException ([62b55a4](https://github.com/ExtendRealityLtd/Malimbe/commit/62b55a4))


### Performance Improvements

* **UnityIntegration:** only reload assemblies that changed ([354296a](https://github.com/ExtendRealityLtd/Malimbe/commit/354296a))

## [8.2.2](https://github.com/ExtendRealityLtd/Malimbe/compare/v8.2.1...v8.2.2) (2019-02-18)


### Bug Fixes

* run weavers on nested types where necessary ([28b4fde](https://github.com/ExtendRealityLtd/Malimbe/commit/28b4fde))
* **XmlToTooltip:** support multiple XML tags in single summary ([049d3fc](https://github.com/ExtendRealityLtd/Malimbe/commit/049d3fc))

## [8.2.1](https://github.com/ExtendRealityLtd/Malimbe/compare/v8.2.0...v8.2.1) (2019-02-16)


### Bug Fixes

* **UnityIntegration:** ensure weaved assemblies are picked up ([9a1c50b](https://github.com/ExtendRealityLtd/Malimbe/commit/9a1c50b))
* **UnityIntegration:** prevent NullReferenceException ([11b1c4e](https://github.com/ExtendRealityLtd/Malimbe/commit/11b1c4e))

# [8.2.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v8.1.4...v8.2.0) (2019-02-13)


### Bug Fixes

* **UnityIntegration:** prevent serialization issues ([3ef85e2](https://github.com/ExtendRealityLtd/Malimbe/commit/3ef85e2))


### Features

* **SerializedProperty:** support properties with just a get or set ([afb09cf](https://github.com/ExtendRealityLtd/Malimbe/commit/afb09cf))

## [8.1.4](https://github.com/ExtendRealityLtd/Malimbe/compare/v8.1.3...v8.1.4) (2019-02-05)


### Bug Fixes

* **UnityIntegration:** stop infinite assembly reload ([9ef91c3](https://github.com/ExtendRealityLtd/Malimbe/commit/9ef91c3))

## [8.1.3](https://github.com/ExtendRealityLtd/Malimbe/compare/v8.1.2...v8.1.3) (2019-02-05)


### Bug Fixes

* **UnityIntegration:** tell Unity to reload assemblies when needed ([6350a71](https://github.com/ExtendRealityLtd/Malimbe/commit/6350a71))

## [8.1.2](https://github.com/ExtendRealityLtd/Malimbe/compare/v8.1.1...v8.1.2) (2019-02-05)


### Bug Fixes

* **UnityIntegration:** delay weaving process until Unity is ready ([c280dc7](https://github.com/ExtendRealityLtd/Malimbe/commit/c280dc7))
* **UnityIntegration:** remove unnecessary .asmdef files ([956a167](https://github.com/ExtendRealityLtd/Malimbe/commit/956a167))

## [8.1.1](https://github.com/ExtendRealityLtd/Malimbe/compare/v8.1.0...v8.1.1) (2019-02-05)


### Bug Fixes

* **PropertyValidation:** support generic base classes ([056b4cd](https://github.com/ExtendRealityLtd/Malimbe/commit/056b4cd))

# [8.1.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v8.0.0...v8.1.0) (2019-02-03)


### Bug Fixes

* **XmlToTooltip:** fall back to manual file lookup for unfound types ([bbb327e](https://github.com/ExtendRealityLtd/Malimbe/commit/bbb327e))
* **XmlToTooltip:** log errors when no documentation was found ([b8fcef9](https://github.com/ExtendRealityLtd/Malimbe/commit/b8fcef9))


### Features

* **PropertySetter:** allow injecting into superclass properties ([a9781c8](https://github.com/ExtendRealityLtd/Malimbe/commit/a9781c8))
* **PropertySetter:** reuse the created local variable ([1e937e7](https://github.com/ExtendRealityLtd/Malimbe/commit/1e937e7))

# [8.0.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v7.0.0...v8.0.0) (2019-01-29)


### Features

* **PropertySetter:** allow using property inside called method ([ba8d4ac](https://github.com/ExtendRealityLtd/Malimbe/commit/ba8d4ac))


### BREAKING CHANGES

* **PropertySetter:** `SetsPropertyAttribute` was renamed to
`CalledBySetterAttribute` and the expected signature of the
annotated method changed. Please see the latest Readme for details.

# [7.0.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v6.1.1...v7.0.0) (2019-01-27)


### Bug Fixes

* **Runner:** combine log level from all configuration files ([8fba11d](https://github.com/ExtendRealityLtd/Malimbe/commit/8fba11d))
* **Runner:** only search for configurations and weavers once ([3f11469](https://github.com/ExtendRealityLtd/Malimbe/commit/3f11469))
* **XmlToTooltip:** don't search for unnecessary type information ([8ea16d9](https://github.com/ExtendRealityLtd/Malimbe/commit/8ea16d9))


### Features

* **Runner:** only run on specified assemblies ([1a45cc6](https://github.com/ExtendRealityLtd/Malimbe/commit/1a45cc6))


### BREAKING CHANGES

* **Runner:** The assemblies to process have to be specified
using the XML _element_ `AssemblyNameRegex` from now on. Specifying
none will result in no assembly being processed and a warning being
logged.

## [6.1.1](https://github.com/ExtendRealityLtd/Malimbe/compare/v6.1.0...v6.1.1) (2019-01-22)

# [6.1.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v6.0.0...v6.1.0) (2019-01-21)


### Bug Fixes

* **packaging:** add missing package files ([7b6ca62](https://github.com/ExtendRealityLtd/Malimbe/commit/7b6ca62))


### Features

* **BehaviourStateRequirement:** allow returning early in Behaviours ([ee9955a](https://github.com/ExtendRealityLtd/Malimbe/commit/ee9955a))

# [6.0.0](https://github.com/ExtendRealityLtd/Malimbe/compare/v5.2.0...v6.0.0) (2019-01-20)


### Code Refactoring

* only run when attribute is used ([3cfdc4b](https://github.com/ExtendRealityLtd/Malimbe/commit/3cfdc4b))


### BREAKING CHANGES

* Any weaver usage now is driven by explicitly
annotating each member that Malimbe's weavers should act on. For
more information read the latest Readme.

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
