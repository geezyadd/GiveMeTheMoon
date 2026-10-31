using Features.PointOfInterestModule.Scripts.Weapons;
using Features.WeaponModule.Contracts;
using UnityEngine;
using Zenject;

namespace Features.PointOfInterestModule.Scripts.Client {
    // The look of one point of interest on every peer (primitives, opaque, so the space haze hides it beyond the clear
    // distance like every rock): its reward container in the middle and the trigger a weapon ray strikes on it (layer
    // FieldRock, as every weapon target). The server never reads it: a reported hit is checked against the point.
    public sealed class PointOfInterestView : MonoBehaviour, IWeaponTargetCollider {
        [SerializeField] private Collider _containerCollider;

        private IPointOfInterestWeaponIds _pointOfInterestWeaponIds;

        public ulong TargetId { get; private set; }

        [Inject]
        private void InjectDependencies(IPointOfInterestWeaponIds pointOfInterestWeaponIds) =>
            _pointOfInterestWeaponIds = pointOfInterestWeaponIds;

        public void Show(int pointId, Vector3 position) {
            transform.position = position;
            TargetId = _pointOfInterestWeaponIds.GetTargetId(pointId);
            _containerCollider.enabled = true;
        }
    }
}
