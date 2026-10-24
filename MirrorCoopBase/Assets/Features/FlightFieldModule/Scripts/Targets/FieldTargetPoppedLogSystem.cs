#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.FlightFieldModule.Contracts;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Debug only, on the server: logs every popped target balloon with its kind, reward, cause and shooter.
    public sealed class FieldTargetPoppedLogSystem : IInitializable, IDisposable {
        private const string LOG_PREFIX = "[FieldTarget] ";

        private readonly IFieldTargetEvents _fieldTargetEvents;

        public FieldTargetPoppedLogSystem(IFieldTargetEvents fieldTargetEvents) =>
            _fieldTargetEvents = fieldTargetEvents;

        public void Initialize() =>
            _fieldTargetEvents.TargetPopped += OnTargetPopped;

        public void Dispose() =>
            _fieldTargetEvents.TargetPopped -= OnTargetPopped;

        private static void OnTargetPopped(FieldTargetPopped popped) =>
            Debug.Log($"{LOG_PREFIX}popped {popped.Id} ({popped.KindId}, +{popped.Reward}) by {popped.Cause}, shooter '{popped.Shooter.Id}', weapon '{popped.WeaponId}', field {popped.FieldNumber}, loop {popped.LoopIndex}");
    }
}
#endif
