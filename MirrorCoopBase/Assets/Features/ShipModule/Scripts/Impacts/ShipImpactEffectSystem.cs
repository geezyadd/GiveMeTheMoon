using System;
using Zenject;

namespace Features.ShipModule.Scripts.Impacts {
    // Every peer: a rock hit plays the pooled impact effect (dust, chunks, the sound hook) at the hit point.
    public sealed class ShipImpactEffectSystem : IInitializable, IDisposable {
        private readonly IShipImpactEvents _shipImpactEvents;
        private readonly IShipImpactEffectPool _shipImpactEffectPool;
        private readonly IShipRockImpactFeelSource _shipRockImpactFeelSource;

        public ShipImpactEffectSystem(IShipImpactEvents shipImpactEvents, IShipImpactEffectPool shipImpactEffectPool, IShipRockImpactFeelSource shipRockImpactFeelSource) {
            _shipImpactEvents = shipImpactEvents;
            _shipImpactEffectPool = shipImpactEffectPool;
            _shipRockImpactFeelSource = shipRockImpactFeelSource;
        }

        public void Initialize() =>
            _shipImpactEvents.OnImpact += OnImpact;

        public void Dispose() =>
            _shipImpactEvents.OnImpact -= OnImpact;

        private void OnImpact(ShipRockImpact impact) =>
            _shipImpactEffectPool.Play(impact.Point, _shipRockImpactFeelSource.GetFeel(impact.Kind).EffectSize);
    }
}
