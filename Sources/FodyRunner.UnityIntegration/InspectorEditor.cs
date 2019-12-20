namespace Malimbe.FodyRunner.UnityIntegration
{
    using Malimbe.MemberChangeMethod;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    /// <summary>
    /// A replacement for the default inspector editor that allows calling change handler methods via <see cref="CalledBeforeChangeOfAttribute"/> and <see cref="CalledAfterChangeOfAttribute"/>.
    /// </summary>
    [CustomEditor(typeof(Object), true)]
    [CanEditMultipleObjects]
    public class InspectorEditor : Editor
    {
        /// <summary>
        /// The key to use to store and retrieve <see cref="UndoRedoWarningPropertyPath"/> into/from <see cref="SessionState"/>.
        /// </summary>
        protected static readonly string UndoRedoWarningSessionStateKey = typeof(InspectorEditor).FullName + nameof(UndoRedoWarningPropertyPath);
        /// <summary>
        /// The script property name.
        /// </summary>
        protected static readonly string ScriptProperty = "m_Script";
        /// <summary>
        /// The message text displayed in the Undo/Redo warning message.
        /// </summary>
        protected static readonly string UndoRedoWarningMessage = "Undo/redo is unsupported for this field at runtime: The change won't be noticed by components depending on it.";
        /// <summary>
        /// <see cref="SerializedProperty.propertyPath"/> of the property changed most recently, <see langword="null"/> if no property has been changed yet.
        /// </summary>
        protected static string UndoRedoWarningPropertyPath
        {
            get => SessionState.GetString(UndoRedoWarningSessionStateKey, null);
            set => SessionState.SetString(UndoRedoWarningSessionStateKey, value);
        }

        /// <summary>
        /// A reusable collection of methods on the current <see cref="SerializedProperty"/>'s declaring type that are annotated with at least one <see cref="HandlesMemberChangeAttribute"/>.
        /// </summary>
        protected readonly List<MethodInfo> ChangeHandlerMethodInfos = new List<MethodInfo>();

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty property = GetObjectProperties();
            if (property == null)
            {
                return;
            }

            ProcessObjectProperties(property);
        }

        /// <summary>
        /// Draws the given property.
        /// </summary>
        /// <param name="property">The property to draw.</param>
        protected virtual void DrawProperty(SerializedProperty property) => EditorGUILayout.PropertyField(property, true);

        /// <summary>
        /// Applies modifications to the given <see cref="SerializedProperty"/>.
        /// </summary>
        /// <param name="property">The property to apply modifications of.</param>
        /// <param name="hasChangeHandlers">Whether <paramref name="property"/> is part of a type that declares a change handler for the property.</param>
        protected virtual void ApplyModifiedProperty(SerializedProperty property, bool hasChangeHandlers)
        {
            if (hasChangeHandlers)
            {
                UndoRedoWarningPropertyPath = property.propertyPath;
            }

            property.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Called before applying changes to the given <see cref="SerializedProperty"/>.
        /// </summary>
        /// <param name="property">The property that is about to change.</param>
        protected virtual void BeforeChange(SerializedProperty property)
        {
            foreach (MethodInfo methodInfo in ChangeHandlerMethodInfos.Where(
                info => info.GetCustomAttributes<CalledBeforeChangeOfAttribute>().Any()))
            {
                methodInfo.Invoke(property.serializedObject.targetObject, null);
            }
        }

        /// <summary>
        /// Called after applying changes to the given <see cref="SerializedProperty"/>.
        /// </summary>
        /// <param name="property">The property that just changed.</param>
        protected virtual void AfterChange(SerializedProperty property)
        {
            foreach (MethodInfo methodInfo in ChangeHandlerMethodInfos.Where(
                    info => info.GetCustomAttributes<CalledAfterChangeOfAttribute>().Any())
                .Reverse())
            {
                methodInfo.Invoke(property.serializedObject.targetObject, null);
            }
        }

        /// <summary>
        /// Finds all methods that are annotated with an attribute inheriting from <see cref="HandlesMemberChangeAttribute"/> and saves them into the reusable <see cref="ChangeHandlerMethodInfos"/> collection.
        /// </summary>
        /// <param name="property">The property that found change handler methods need to be annotated for.</param>
        protected virtual void FindChangeHandlerMethods(SerializedProperty property)
        {
            ChangeHandlerMethodInfos.Clear();
            ChangeHandlerMethodInfos.AddRange(
                property.serializedObject.targetObject.GetType()
                    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                    .Where(
                        info => info.GetCustomAttributes<HandlesMemberChangeAttribute>(true)
                            .Any(attribute => IsAttributeForProperty(property, info, attribute))));
        }

        /// <summary>
        /// Whether the given <see cref="HandlesMemberChangeAttribute"/> is configured to run the method its define on for the given property.
        /// </summary>
        /// <param name="property">The property to try to match against.</param>
        /// <param name="methodInfo">The method the attribute is defined on.</param>
        /// <param name="attribute">The attribute to run the matching logic on.</param>
        /// <returns>Whether to run the annotated method <paramref name="methodInfo"/>.</returns>
        protected virtual bool IsAttributeForProperty(
            SerializedProperty property,
            MethodInfo methodInfo,
            HandlesMemberChangeAttribute attribute)
        {
            string propertyPath = property.propertyPath;
            if (attribute.DataMemberName == propertyPath)
            {
                return true;
            }

            char firstChar = propertyPath[0];
            firstChar = char.IsLower(firstChar) ? char.ToUpper(firstChar) : char.ToLower(firstChar);
            string alternativePropertyPath = firstChar + propertyPath.Substring(1);
            if (attribute.DataMemberName != alternativePropertyPath)
            {
                return false;
            }

            Type type = methodInfo.DeclaringType;
            return type?.GetProperty(
                    alternativePropertyPath,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                != null
                && GetPrivateField(type, propertyPath)?.GetCustomAttribute<SerializeField>() != null;
        }

        /// <summary>
        /// Finds a private field on a type, even if it's defined on a base type.
        /// </summary>
        /// <param name="type">The type to search on.</param>
        /// <param name="name">The name of the field to search for.</param>
        /// <returns>The found field or <see langword="null"/> if no field was found.</returns>
        protected virtual FieldInfo GetPrivateField(Type type, string name)
        {
            const BindingFlags bindingFlags =
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            FieldInfo fieldInfo;
            do
            {
                fieldInfo = type.GetField(name, bindingFlags);
                type = type.BaseType;
            }
            while (fieldInfo == null && type != null);

            return fieldInfo;
        }

        /// <summary>
        /// Attempts to retrieve all properties associated to the <see cref="serializedObject"/>.
        /// </summary>
        /// <returns>A <see cref="SerializedProperty"/> collection.</returns>
        protected virtual SerializedProperty GetObjectProperties()
        {
            SerializedProperty property = serializedObject.GetIterator();
            if (!property.NextVisible(true))
            {
                return null;
            }

            return property;
        }

        /// <summary>
        /// Whether to show the Undo/Redo warning in the inspector.
        /// </summary>
        /// <param name="propertyPath">The current path of the property to check.</param>
        /// <param name="undoRedoWarningPropertyPath">The path to the undo/redo warning for the property.</param>
        /// <returns>Whether the Undo/Redo warning should be shown.</returns>
        protected virtual bool CanShowUndoRedoWarning(string propertyPath, string undoRedoWarningPropertyPath)
        {
            return Application.isPlaying && propertyPath == undoRedoWarningPropertyPath;
        }

        /// <summary>
        /// Whether to apply the change handlers for the property.
        /// </summary>
        /// <param name="targetObject">The target object to check.</param>
        /// <returns>Whether the change handler can be applied.</returns>
        protected virtual bool CanApplyChangeHandlers(Object targetObject)
        {
            return Application.isPlaying && targetObject is Behaviour behaviour && behaviour.isActiveAndEnabled;
        }

        /// <summary>
        /// Attempts to apply any found change handlers to the given property for the target object.
        /// </summary>
        /// <param name="targetObject">The target object to apply on.</param>
        /// <param name="property">The property to apply the change handlers for.</param>
        /// <returns>Whether the change handler was successfully applied to the property.</returns>
        protected virtual bool TryApplyChangeHandlersToProperty(Object targetObject, SerializedProperty property)
        {
            // At design time we need to still allow Unity to persist the change and enable undo.
            if (!CanApplyChangeHandlers(targetObject))
            {
                ApplyModifiedProperty(property, false);
                return false;
            }

            ApplyChangeHandlersToProperty(targetObject, property);
            return true;
        }

        /// <summary>
        /// Applies any found change handlers to the given property.
        /// </summary>
        /// <param name="targetObject">The target object to apply on.</param>
        /// <param name="property">The property to apply the change handlers for.</param>
        protected virtual void ApplyChangeHandlersToProperty(Object targetObject, SerializedProperty property)
        {
            FindChangeHandlerMethods(property);

            // There are change handlers. Run them manually and ensure Unity knows those change handlers did some changes (to persist them and enable undo for them).
            if (ChangeHandlerMethodInfos.Count > 0)
            {
                Undo.RecordObject(targetObject, "Before change handlers");
                BeforeChange(property);
                Undo.FlushUndoRecordObjects();

                using (SerializedObject serializedObjectCopy =
                    new SerializedObject(property.serializedObject.targetObject))
                {
                    SerializedProperty propertyCopy = serializedObjectCopy.GetIterator();
                    if (propertyCopy.Next(true))
                    {
                        do
                        {
                            if (propertyCopy.propertyPath != property.propertyPath)
                            {
                                property.serializedObject.CopyFromSerializedProperty(propertyCopy);
                            }
                        }
                        while (propertyCopy.Next(false));
                    }
                }

                ApplyModifiedProperty(property, true);
                AfterChange(property);
            }
            // Ensure subclasses of this inspector can run before/after change logic. Also ensure Unity persists the change (including undo support).
            else
            {
                BeforeChange(property);
                ApplyModifiedProperty(property, false);
                AfterChange(property);
            }
        }

        /// <summary>
        /// Processes the properties on the given <see cref="SerializedProperty"/> collection.
        /// </summary>
        /// <param name="property">A collection of properties to process.</param>
        protected virtual void ProcessObjectProperties(SerializedProperty property)
        {
            string undoRedoWarningPropertyPath = UndoRedoWarningPropertyPath;

            do
            {
                string propertyPath = property.propertyPath;
                Object targetObject = property.serializedObject.targetObject;

                using (EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope())
                using (new EditorGUI.DisabledGroupScope(propertyPath == ScriptProperty))
                {
                    bool showUndoRedoWarning = CanShowUndoRedoWarning(propertyPath, undoRedoWarningPropertyPath);
                    BeginDrawWarningMessage(showUndoRedoWarning);
                    DrawProperty(property);
                    EndDrawWarningMessage(showUndoRedoWarning);

                    // No change has been done, nothing to do.
                    if (!changeCheckScope.changed)
                    {
                        continue;
                    }

                    TryApplyChangeHandlersToProperty(targetObject, property);
                }
            }
            while (property.NextVisible(false));
        }

        /// <summary>
        /// Initiates the drawing the Undo/Redo warning.
        /// </summary>
        /// <param name="show">Whether to show the warning.</param>
        protected virtual void BeginDrawWarningMessage(bool show)
        {
            if (show)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.HelpBox(UndoRedoWarningMessage, MessageType.Warning);
                EditorGUI.indentLevel++;
            }
        }

        /// <summary>
        /// Concludes the drawing the Undo/Redo warning.
        /// </summary>
        /// <param name="show">Whether to show the warning.</param>
        protected virtual void EndDrawWarningMessage(bool show)
        {
            if (show)
            {
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
        }
    }
}
