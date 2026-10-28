using Features.WeaponModule.Contracts;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Overboard {
    // Every peer: while a rope holds the drifting player, the line from the hooker's weapon (as this peer shows it)
    // to the player.
    public sealed class ShipOverboardRope : MonoBehaviour {
        private const int ROPE_POINT_COUNT = 2;

        [SerializeField] private ShipOverboardRider _overboardRider;
        [SerializeField] private LineRenderer _line;

        private IWeaponHolderAnchors _weaponHolderAnchors;

        [Inject]
        private void InjectDependencies(IWeaponHolderAnchors weaponHolderAnchors) =>
            _weaponHolderAnchors = weaponHolderAnchors;

        private void Awake() {
            _line.useWorldSpace = true;
            _line.positionCount = ROPE_POINT_COUNT;
            _line.enabled = false;
        }

        private void LateUpdate() {
            bool isShown = TryGetHookerAnchor(out WeaponHolderAnchor anchor);
            _line.enabled = isShown;
            if (isShown == false)
                return;

            _line.SetPosition(0, anchor.Origin);
            _line.SetPosition(1, _overboardRider.Position);
        }

        private bool TryGetHookerAnchor(out WeaponHolderAnchor anchor) {
            anchor = default;
            return _overboardRider.IsOverboard && _overboardRider.IsHooked && _weaponHolderAnchors.TryGetAnchor(_overboardRider.HookerKey, out anchor);
        }
    }
}
