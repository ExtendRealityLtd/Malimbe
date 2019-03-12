namespace Malimbe.FodyRunner.UnityIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Malimbe.MemberChangeMethod;
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
        /// A reusable collection of methods on the current <see cref="SerializedProperty"/>'s declaring type that are annotated with at least one <see cref="HandlesMemberChangeAttribute"/>.
        /// </summary>
        protected readonly List<MethodInfo> ChangeHandlerMethodInfos = new List<MethodInfo>();

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty property = serializedObject.GetIterator();
            if (!property.NextVisible(true))
            {
                return;
            }

            do
            {
                string propertyPath = property.propertyPath;
                Object targetObject = property.serializedObject.targetObject;

                ChangeHandlerMethodInfos.Clear();
                ChangeHandlerMethodInfos.AddRange(
                    targetObject.GetType()
                        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(
                            info => info.GetCustomAttributes<HandlesMemberChangeAttribute>()
                                .Any(attribute => IsAttributeForProperty(property, info, attribute))));

                using (EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope())
                using (new EditorGUI.DisabledGroupScope(propertyPath == "m_Script"))
                {
                    DrawProperty(property);

                    if (!changeCheckScope.changed
                        || !Application.isPlaying
                        || targetObject is Behaviour behaviour && !behaviour.isActiveAndEnabled)
                    {
                        if (changeCheckScope.changed)
                        {
                            Debug.Log(propertyPath);
                            ApplyModifiedProperty(property, ChangeHandlerMethodInfos.Count > 0);
                        }

                        continue;
                    }
                }

                BeforeChange(property);
                ApplyModifiedProperty(property, true);
                AfterChange(property);
            }
            while (property.NextVisible(false));
        }

        /// <summary>
        /// Draws the given property.
        /// </summary>
        /// <param name="property">The property to draw.</param>
        protected virtual void DrawProperty(SerializedProperty property) =>
            EditorGUILayout.PropertyField(property, true);

        /// <summary>
        /// Applies modifications to the given <see cref="SerializedProperty"/>.
        /// </summary>
        /// <param name="property">The property to apply modifications of.</param>
        /// <param name="hasChangeHandlers">Whether <paramref name="property"/> is part of a type that declares a change handler for the property.</param>
        protected virtual void ApplyModifiedProperty(SerializedProperty property, bool hasChangeHandlers)
        {
            if (hasChangeHandlers)
            {
                // Change handlers can't be called for undo or redo operations, so don't register an undo operation.
                property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Called before applying changes to the given <see cref="SerializedProperty"/>.
        /// </summary>
        /// <param name="property">The property that is about to change.</param>
        protected virtual void BeforeChange(SerializedProperty property)
        {
            foreach (MethodInfo methodInfo in ChangeHandlerMethodInfos.Where(
                info => info.GetCustomAttribute<CalledBeforeChangeOfAttribute>() != null))
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
                    info => info.GetCustomAttribute<CalledAfterChangeOfAttribute>() != null)
                .Reverse())
            {
                methodInfo.Invoke(property.serializedObject.targetObject, null);
            }
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
    }
}
