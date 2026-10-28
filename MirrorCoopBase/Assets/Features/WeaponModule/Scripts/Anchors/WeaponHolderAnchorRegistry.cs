using System;
using System.Collections.Generic;
using Features.NetworkModelModule.Scripts;
using Features.WeaponModule.Contracts;
using Features.WeaponModule.Scripts.Parts.Holding;

namespace Features.WeaponModule.Scripts.Anchors {
    // The weapons in the players' hands on this peer, by player key: the weapon and its live aim, so an anchor is
    // always where the muzzle is now. A player holds one weapon at a time.
    public sealed class WeaponHolderAnchorRegistry : IWeaponHolderAnchors, IWeaponHolderAnchorRegistry {
        private const string DUPLICATE_HOLDER_FORMAT = "Weapon holder {0} is registered twice.";

        private readonly Dictionary<PlayerKey, WeaponHolderEntry> _holders = new Dictionary<PlayerKey, WeaponHolderEntry>();

        public void Register(PlayerKey holder, string weaponId, IWeaponAim aim) {
            if (_holders.ContainsKey(holder))
                throw new InvalidOperationException(string.Format(DUPLICATE_HOLDER_FORMAT, holder));

            _holders.Add(holder, new WeaponHolderEntry(weaponId, aim));
        }

        public void Unregister(PlayerKey holder) =>
            _holders.Remove(holder);

        public bool TryGetAnchor(PlayerKey holder, out WeaponHolderAnchor anchor) {
            anchor = default;
            if (!_holders.TryGetValue(holder, out WeaponHolderEntry entry))
                return false;

            anchor = new WeaponHolderAnchor(entry.WeaponId, entry.Aim.Origin);
            return true;
        }
    }
}
