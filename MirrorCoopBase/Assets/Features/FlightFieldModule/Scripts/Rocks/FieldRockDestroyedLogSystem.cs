#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.FlightFieldModule.Contracts;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Editor / development only: logs every rock the server breaks, with everything the event carries.
    public sealed class FieldRockDestroyedLogSystem : IInitializable, IDisposable {
        private const string LOG_PREFIX = "[FieldRock] destroyed ";

        private readonly IFieldRockEvents _fieldRockEvents;

        public FieldRockDestroyedLogSystem(IFieldRockEvents fieldRockEvents) =>
            _fieldRockEvents = fieldRockEvents;

        public void Initialize() =>
            _fieldRockEvents.RockDestroyed += OnRockDestroyed;

        public void Dispose() =>
            _fieldRockEvents.RockDestroyed -= OnRockDestroyed;

        private void OnRockDestroyed(FieldRockDestroyed destroyed) =>
            Debug.Log($"{LOG_PREFIX}{destroyed.Id} ({destroyed.SourceId}, {destroyed.Size}, {destroyed.Diameter:F1} m) at {destroyed.Position} by {destroyed.Cause}, "
                      + $"instigator '{destroyed.Instigator}', weapon '{destroyed.WeaponId}', field {destroyed.FieldNumber}, loop {destroyed.LoopIndex}");
    }
}
#endif
