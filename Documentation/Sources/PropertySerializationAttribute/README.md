# `PropertySerializationAttribute`

A Unity software specific weaver. Ensures the backing field for a property is serialized.

* Annotate a property with `[Serialized]` to use this. The property needs at least a getter _or_ a setter.
* If the property's backing field doesn't use `[SerializeField]` it will be added.
* If the property is an [auto-implemented property][Auto-Implemented Property] the backing field will be renamed to match the property's name for viewing in the Unity software inspector. All backing field usages inside methods of the declaring type will be updated to use this new name. Since C# doesn't allow multiple members of a type to share a name, the backing field's name will differ in the first character's case. E.g.:
  * `public int Counter { get; set; }` will use a backing field called `counter`.
  * `protected bool isValid { get; private set; }` will use a backing field called `IsValid`.

[Auto-Implemented Property]: https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/auto-implemented-properties