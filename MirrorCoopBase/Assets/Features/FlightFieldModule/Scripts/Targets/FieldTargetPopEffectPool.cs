using Features.EffectPoolModule.Scripts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.GameCoreModule.Contracts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Pop effects made once and played again, under the parent the caller gives; the coins fly to this peer's ship and
    // the "+N" faces this peer's viewpoint.
    public sealed class FieldTargetPopEffectPool : IFieldTargetPopEffectPool {
        private readonly FieldTargetConfiguration _fieldTargetConfiguration;
        private readonly IShipPositionSource _shipPositionSource;
        private readonly ILocalViewpointSource _localViewpointSource;
        private readonly SelfDisablingEffectPool<FieldTargetPopEffect> _effects = new();

        public FieldTargetPopEffectPool(FieldTargetConfiguration fieldTargetConfiguration, IShipPositionSource shipPositionSource, ILocalViewpointSource localViewpointSource) {
            _fieldTargetConfiguration = fieldTargetConfiguration;
            _shipPositionSource = shipPositionSource;
            _localViewpointSource = localViewpointSource;
        }

        public void Play(Transform parent, Vector3 position, FieldTargetDefinition kind) =>
            _effects.TakeFree(_fieldTargetConfiguration.PopEffect, parent).Play(position, kind.RadiusMeters, kind.Reward, kind.PopColor, _fieldTargetConfiguration.PopFeel,
                _shipPositionSource, _localViewpointSource);
    }
}
