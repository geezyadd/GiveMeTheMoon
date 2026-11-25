using Features.ShipModule.Scripts.Contracts;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Overboard {
    // A piece of ground a player can drift away from when the ship is not flying (the lobby's floor, its test platform,
    // a station's deck): its space is its floors' live bounds widened like the ship's volume (ShipOverboardRule), so
    // it follows a moving platform. A player put back after drifting too far appears at the return point. In a scene
    // with a context it adds itself to the area registry; a station pad is spawned without one, and the overboard
    // system takes its area from the current pad instead.
    public sealed class OverboardArea : MonoBehaviour {
        [Tooltip("The colliders of the floor players stand on here.")]
        [SerializeField] private Collider[] _floors = new Collider[0];
        [Tooltip("Where a player who drifted too far is put back. Empty: the standing ship's deck spawn point.")]
        [SerializeField] private Transform _returnPoint;
        private IComponentRegistry<OverboardArea> _registry;
        private bool _isRegistered;

        public bool HasReturnPoint => _returnPoint != null;
        public Pose ReturnPose => new(_returnPoint.position, _returnPoint.rotation);
        [Inject]
        private void InjectDependencies(IComponentRegistry<OverboardArea> registry) {
            _registry = registry;
            Register();
        }

        private void OnEnable() =>
            Register();

        private void OnDisable() {
            if (_isRegistered)
                _registry.Remove(this);
            _isRegistered = false;
        }

        public bool Contains(IShipOverboardRule rule, Vector3 point) {
            foreach (Collider floor in _floors)
                if (floor != null && floor.enabled && rule.IsInArea(floor.bounds, point))
                    return true;
            return false;
        }

        public bool IsOverFloor(IShipOverboardRule rule, Vector3 point) {
            foreach (Collider floor in _floors)
                if (floor != null && floor.enabled && rule.IsOverFloor(floor.bounds, point))
                    return true;
            return false;
        }

        // From the point to the nearest floor's bounds, meters (0 inside them).
        public float DistanceTo(Vector3 point) {
            float nearest = float.PositiveInfinity;
            foreach (Collider floor in _floors)
                if (floor != null)
                    nearest = Mathf.Min(nearest, Mathf.Sqrt(floor.bounds.SqrDistance(point)));
            return nearest;
        }

        private void Register() {
            if (_isRegistered || _registry == null || isActiveAndEnabled == false)
                return;

            _registry.Add(this);
            _isRegistered = true;
        }
    }
}
