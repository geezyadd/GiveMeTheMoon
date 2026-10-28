#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.DebugModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;
using Features.GameCoreModule.Contracts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Ore {
    // Debug only: the debug window's "Drop ore before the ship" action, on the server (host), drops a few pieces of
    // ore ahead of the ship along the field, as a broken rock does, to try the harpoon without breaking a rock.
    public sealed class FieldOreDropDebugSystem : IInitializable, IDisposable {
        private const string LOG_PREFIX = "[FieldOre] Debug drop: ";
        private const string DROP_LABEL = "Drop ore before the ship";
        private const string DROP_DESCRIPTION = "Drops a few pieces of ore ahead of the ship along the flight field, as a broken rock does (host, in flight).";
        private const string NO_FIELD_REASON = "No flight field.";
        // The pieces start together and drift apart.
        private const float NO_SCATTER = 0f;

        private readonly IDebugActionRegistry _debugActionRegistry;
        private readonly IFieldOreDropService _fieldOreDropService;
        private readonly IFieldOres _fieldOres;
        private readonly IShipPositionSource _shipPositionSource;
        private readonly FieldOreConfiguration _fieldOreConfiguration;
        private readonly IFieldRandom _random;

        private IDisposable _registration;

        public FieldOreDropDebugSystem(IDebugActionRegistry debugActionRegistry, IFieldOreDropService fieldOreDropService, IFieldOres fieldOres,
                                       IShipPositionSource shipPositionSource, FieldOreConfiguration fieldOreConfiguration, IFieldRandomFactory fieldRandomFactory) {
            _debugActionRegistry = debugActionRegistry;
            _fieldOreDropService = fieldOreDropService;
            _fieldOres = fieldOres;
            _shipPositionSource = shipPositionSource;
            _fieldOreConfiguration = fieldOreConfiguration;
            _random = fieldRandomFactory.Create(Environment.TickCount);
        }

        public void Initialize() =>
            _registration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.ROCKS, DROP_LABEL, DROP_DESCRIPTION, ReadUnavailableReason, DropOre));

        public void Dispose() =>
            _registration.Dispose();

        private string ReadUnavailableReason() {
            if (!NetworkServer.active)
                return DebugActionReasons.HOST_ONLY;

            return _fieldOres.HasField ? null : NO_FIELD_REASON;
        }

        private void DropOre() {
            if (!_shipPositionSource.TryGetShipPosition(out Vector3 shipPosition))
                return;

            Vector3 position = shipPosition + _fieldOres.Rotation * Vector3.forward * _fieldOreConfiguration.DebugDropDistanceMeters
                + Vector3.up * _fieldOreConfiguration.DebugDropHeightMeters;
            int dropped = _fieldOreDropService.ServerDropAt(position, NO_SCATTER, _fieldOreConfiguration.DebugDropCount, _random);
            Debug.Log($"{LOG_PREFIX}{dropped} at {position}");
        }
    }
}
#endif
