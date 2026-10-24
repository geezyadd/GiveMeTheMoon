using UnityEngine;

namespace Features.WeaponModule.Scripts.Definitions {
    // Marks a [SerializeReference] weapon part field: the inspector shows a menu of every serializable part type that
    // fits the field, so a designer picks a part without code.
    public sealed class WeaponPartPickerAttribute : PropertyAttribute {
    }
}
