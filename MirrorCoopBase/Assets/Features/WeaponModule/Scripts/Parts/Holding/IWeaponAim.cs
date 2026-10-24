using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Holding {
    // Where a shot starts and where it goes: a turret barrel, a player camera, a muzzle in a hand.
    public interface IWeaponAim {
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }
    }
}
