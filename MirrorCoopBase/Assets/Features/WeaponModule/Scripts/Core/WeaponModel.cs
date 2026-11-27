using System;
using Features.WeaponModule.Scripts.Network;

namespace Features.WeaponModule.Scripts.Core {
    // The local player's weapon in hand or on a turret, and that player's holder that fires it. Local: every peer
    // knows only its own player's weapon here; the other players' weapons live on their holders.
    public sealed class WeaponModel {
        public IWeapon Current { get; private set; }
        public WeaponHolderSync Holder { get; private set; }

        public event Action<IWeapon> CurrentChanged;

        public void SetCurrent(IWeapon weapon, WeaponHolderSync holder) {
            Current = weapon;
            Holder = weapon != null ? holder : null;
            CurrentChanged?.Invoke(weapon);
        }
    }
}
