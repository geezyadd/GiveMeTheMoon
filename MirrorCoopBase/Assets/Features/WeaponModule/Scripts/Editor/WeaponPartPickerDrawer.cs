using System;
using System.Linq;
using Features.WeaponModule.Scripts.Definitions;
using UnityEditor;
using UnityEngine;

namespace Features.WeaponModule.Editor {
    // Draws a [SerializeReference] weapon part with a type menu next to its label: every concrete [Serializable] type
    // that fits the field, plus "None".
    [CustomPropertyDrawer(typeof(WeaponPartPickerAttribute))]
    public sealed class WeaponPartPickerDrawer : PropertyDrawer {
        private const string NONE_LABEL = "None";
        private const char TYPE_NAME_SEPARATOR = ' ';

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUI.GetPropertyHeight(property, label, includeChildren: true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            Rect menuRect = new Rect(
                position.x + EditorGUIUtility.labelWidth,
                position.y,
                position.width - EditorGUIUtility.labelWidth,
                EditorGUIUtility.singleLineHeight);
            if (EditorGUI.DropdownButton(menuRect, new GUIContent(ReadCurrentTypeName(property)), FocusType.Keyboard))
                ShowTypeMenu(property);

            EditorGUI.PropertyField(position, property, label, includeChildren: true);
        }

        private static string ReadCurrentTypeName(SerializedProperty property) =>
            property.managedReferenceValue == null
                ? NONE_LABEL
                : ObjectNames.NicifyVariableName(property.managedReferenceValue.GetType().Name);

        private static void ShowTypeMenu(SerializedProperty property) {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent(NONE_LABEL), property.managedReferenceValue == null, AssignPart, new WeaponPartChoice(property, null));
            foreach (Type type in FindPartTypes(property))
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), property.managedReferenceValue?.GetType() == type, AssignPart, new WeaponPartChoice(property, type));
            menu.ShowAsContext();
        }

        private static Type[] FindPartTypes(SerializedProperty property) {
            Type fieldType = ReadFieldType(property);
            return TypeCache.GetTypesDerivedFrom(fieldType)
                .Where(IsAssignablePart)
                .OrderBy(type => type.Name)
                .ToArray();
        }

        private static bool IsAssignablePart(Type type) =>
            type.IsAbstract == false
            && type.IsInterface == false
            && type.IsGenericType == false
            && type.IsSerializable
            && typeof(UnityEngine.Object).IsAssignableFrom(type) == false
            && type.GetConstructor(Type.EmptyTypes) != null;

        // managedReferenceFieldTypename is "<assembly> <full type name>".
        private static Type ReadFieldType(SerializedProperty property) {
            string[] parts = property.managedReferenceFieldTypename.Split(TYPE_NAME_SEPARATOR);
            return Type.GetType($"{parts[1]}, {parts[0]}", throwOnError: true);
        }

        private static void AssignPart(object userData) {
            WeaponPartChoice choice = (WeaponPartChoice)userData;
            SerializedProperty property = choice.SerializedObject.FindProperty(choice.PropertyPath);
            choice.SerializedObject.Update();
            property.managedReferenceValue = choice.Type == null ? null : Activator.CreateInstance(choice.Type);
            choice.SerializedObject.ApplyModifiedProperties();
        }
    }
}
