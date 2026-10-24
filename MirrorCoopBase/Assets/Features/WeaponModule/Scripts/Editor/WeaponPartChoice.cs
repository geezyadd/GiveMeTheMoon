using System;
using UnityEditor;

namespace Features.WeaponModule.Editor {
    // One entry of the weapon part menu: which field gets which part type (null = none). Keeps the path, not the
    // property, because the menu answers after the inspector has redrawn.
    public sealed class WeaponPartChoice {
        public SerializedObject SerializedObject { get; }
        public string PropertyPath { get; }
        public Type Type { get; }

        public WeaponPartChoice(SerializedProperty property, Type type) {
            SerializedObject = property.serializedObject;
            PropertyPath = property.propertyPath;
            Type = type;
        }
    }
}
