using Features.WeaponModule.Scripts.Parts.Holding;
using UnityEngine;

namespace Features.WeaponModule.Editor.Tests {
    // An aim that stands still at the origin, looking forward.
    public sealed class FixedWeaponHolder : IWeaponAim {
        public Vector3 Origin => Vector3.zero;
        public Vector3 Direction => Vector3.forward;
    }
}
