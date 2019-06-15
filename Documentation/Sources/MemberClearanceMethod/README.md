# `MemberClearanceMethod`

A generic weaver. Creates `ClearMemberName()` methods for any member `MemberName` that is of reference type. Sets the member to `null` in this method.

* Annotate a member with `[Cleared]` to use this. Both properties and fields are supported. Properties need a setter.
* Instead of `ClearMemberName` the method name's _prefix_ can be customized with the XML _attribute_ `MethodNamePrefix`, e.g.:
  ```xml
    <Malimbe.MemberClearanceMethod MethodNamePrefix="Nullify" />
  ```
  This will create methods named `NullifyMemberName`.
* In case the method already exists the instructions will be weaved into the _end_ of the method. The method name lookup is case insensitive.