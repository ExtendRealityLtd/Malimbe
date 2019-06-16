# `BehaviourStateRequirementMethod`

A Unity software specific weaver. Changes a method to return early if a combination of the GameObject's active state and the Behaviour's enabled state doesn't match the configured state.

* Annotate a method with `[RequiresBehaviourState]` to use this. The method needs to be defined in a type that derives from `UnityEngine.Behaviour`, e.g. a `MonoBehaviour`.
* Use the attribute constructor's parameters to configure the specific state you need the GameObject and the Behaviour to be in.