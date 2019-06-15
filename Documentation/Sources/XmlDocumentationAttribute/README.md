# `XmlDocumentationAttribute`

A generic weaver (though made for the Unity software). Looks up the XML `<summary>` documentation for a field and adds `[Tooltip]` to that field with that summary.

* Annotate a field with `[DocumentedByXml]` to use this.
* Instead of `TooltipAttribute` the added attribute can be customized with the XML _attribute_ `FullAttributeName`, e.g.:
  ```xml
    <Malimbe.XmlDocumentationAttribute FullAttributeName="Some.Other.Namespace.DocumentationAttribute" />
  ```
  The attribute needs to have a constructor that takes a `string` parameter and nothing else. Note that the attribute name has to be the full type name, i.e. prefixed by the namespace.
* In case the attribute already exists on the field it will be replaced.
* Tags in the XML documentation comment like `<see cref="Something"/>` will be replaced by just the "identifier" `Something` by default. To customize this behavior the XML _attribute_ `IdentifierReplacementFormat` can be used, e.g.:
  ```xml
    <Malimbe.XmlDocumentationAttribute IdentifierReplacementFormat="`{0}`" />
  ```
  The format needs to specify a placeholder `{0}`, otherwise an error will be logged and the default replacement format will be used instead.