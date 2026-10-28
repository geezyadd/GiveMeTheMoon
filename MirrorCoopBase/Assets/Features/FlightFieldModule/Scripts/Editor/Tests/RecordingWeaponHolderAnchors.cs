using System.Collections.Generic;
using Features.NetworkModelModule.Scripts;
using Features.WeaponModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    public sealed class RecordingWeaponHolderAnchors : IWeaponHolderAnchors {
        private readonly Dictionary<PlayerKey, WeaponHolderAnchor> _anchors = new();

        public void Set(PlayerKey holder, WeaponHolderAnchor anchor) =>
            _anchors[holder] = anchor;

        public void Remove(PlayerKey holder) =>
            _anchors.Remove(holder);

        public bool TryGetAnchor(PlayerKey holder, out WeaponHolderAnchor anchor) =>
            _anchors.TryGetValue(holder, out anchor);
    }
}
