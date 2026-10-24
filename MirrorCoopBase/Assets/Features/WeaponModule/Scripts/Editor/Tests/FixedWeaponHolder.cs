using Features.WeaponModule.Scripts.Parts.Holding;
using UnityEngine;

namespace Features.WeaponModule.Editor.Tests {
    // A holder whose aim stands still at the origin, looking forward.
    public sealed class FixedWeaponHolder : IWeaponHolder, IWeaponAim {
        public bool IsTriggerHeld { get; set; }
        public IWeaponAim Aim => this;
        public Vector3 Origin => Vector3.zero;
        public Vector3 Direction => Vector3.forward;
    }
}
