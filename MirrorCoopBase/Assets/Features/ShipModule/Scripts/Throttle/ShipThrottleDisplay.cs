using Features.ShipModule.Scripts.Generated;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Throttle {
    // Tilts the handle along its arc: the holder sees its own hand at once, everyone else sees the synced throttle
    // eased in, so the handle moves smoothly between the holder's updates.
    public sealed class ShipThrottleDisplay : MonoBehaviour {
        [SerializeField] private Transform _handlePivot;
        [SerializeField] private ShipThrottleInteractable _shipThrottleInteractable;

        private IReadOnlyShipThrottleModel _shipThrottleModel;
        private ShipThrottleConfiguration _shipThrottleConfiguration;
        private float _shownPosition;
        private float _shownVelocity;

        [Inject]
        private void InjectDependencies(IReadOnlyShipThrottleModel shipThrottleModel, ShipThrottleConfiguration shipThrottleConfiguration) {
            _shipThrottleModel = shipThrottleModel;
            _shipThrottleConfiguration = shipThrottleConfiguration;
        }

        private void LateUpdate() {
            if (_shipThrottleInteractable.TryGetLocalPosition(out float localPosition)) {
                _shownPosition = localPosition;
                _shownVelocity = 0f;
            }
            else {
                _shownPosition = Mathf.SmoothDamp(
                    _shownPosition,
                    _shipThrottleModel.Position,
                    ref _shownVelocity,
                    _shipThrottleConfiguration.RemoteSmoothSeconds);
            }

            float angle = Mathf.Lerp(_shipThrottleConfiguration.StopAngle, _shipThrottleConfiguration.FullAngle, _shownPosition);
            _handlePivot.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
    }
}
