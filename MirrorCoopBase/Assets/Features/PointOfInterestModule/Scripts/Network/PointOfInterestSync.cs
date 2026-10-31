using Features.PointOfInterestModule.Scripts.Network.Generated;
using Features.ShipModule.Scripts.Contracts;
using Zenject;

namespace Features.PointOfInterestModule.Scripts.Network {
    // The PointsOfInterest bridge as a component on the ship's network identity (a generated bridge file cannot be
    // added to a GameObject itself); the server finds it through the registry to write the points.
    public sealed class PointOfInterestSync : PointsOfInterestBridge {
        private IComponentRegistry<PointOfInterestSync> _pointOfInterestSyncRegistry;

        [Inject]
        private void InjectDependencies(IComponentRegistry<PointOfInterestSync> pointOfInterestSyncRegistry) =>
            _pointOfInterestSyncRegistry = pointOfInterestSyncRegistry;

        private void OnEnable() =>
            _pointOfInterestSyncRegistry.Add(this);

        private void OnDisable() =>
            _pointOfInterestSyncRegistry.Remove(this);
    }
}
