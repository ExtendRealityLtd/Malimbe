# `MemberChangeMethod`

A Unity software specific weaver. Calls a method before or after a data member (field or property) is changed.

* Annotate a method with `[CalledBeforeChangeOf(nameof(SomeFieldOrProperty))]` (or `CalledAfterChangeOfAttribute`) to use this. The accessibility level of the method doesn't matter and the name lookup is case insensitive.
* The method needs to follow the signature pattern `void MethodName()`. Use the data member's accessor in the method body to retrieve the current value. The method will only be called when [`Application.isPlaying`][Application.isPlaying] is `true`.
* The referenced data member needs to be declared in the same type the method is declared in. For a property member a getter is required.
* A custom Editor `InspectorEditor` is part of `FodyRunner.UnityIntegration` and is automatically used to draw the inspector for any type that doesn't use a custom editor. This custom editor calls the configured methods on change of a data member annotated with one of the two attributes above.
  * Note that this is only done when the Editor is playing, as changes at design time should be handled by using [`PropertyAttribute`][PropertyAttribute]s and calling the same method that uses `CalledAfterChangeOfAttribute` for this data member in `OnEnable` of the declaring type. With that in place the data member's state will properly be handled, right at startup and from there on by the annotated change handling methods.
  * Inherit from `InspectorEditor` in custom editors for types that use one of the two attributes above and override the method `DrawProperty`.

[Application.isPlaying]: https://docs.unity3d.com/ScriptReference/Application-isPlaying.html
[PropertyAttribute]: https://docs.unity3d.com/ScriptReference/PropertyAttribute.html