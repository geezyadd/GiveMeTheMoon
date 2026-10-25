using System;
using System.Collections.Generic;
using Features.ShipModule.Scripts.Contracts;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // The deck spawn points of the ship in the loaded map, handed out in turn.
    public sealed class ShipDeckSpawnPointRegistry : IShipDeckSpawnPoints {
        private const string NO_POINT_ERROR = "The ship has no " + nameof(ShipDeckSpawnPoint) + ".";

        private readonly IReadOnlyComponentRegistry<ShipDeckSpawnPoint> _shipDeckSpawnPointRegistry;
        private int _nextIndex;

        public bool HasSpawnPoint =>
            Points.Count > 0;

        private IReadOnlyList<ShipDeckSpawnPoint> Points =>
            _shipDeckSpawnPointRegistry.Items;

        public ShipDeckSpawnPointRegistry(IReadOnlyComponentRegistry<ShipDeckSpawnPoint> shipDeckSpawnPointRegistry) =>
            _shipDeckSpawnPointRegistry = shipDeckSpawnPointRegistry;

        public Pose TakeSpawnPose() {
            if (HasSpawnPoint == false)
                throw new InvalidOperationException(NO_POINT_ERROR);

            Transform point = Points[_nextIndex % Points.Count].transform;
            _nextIndex = (_nextIndex + 1) % Points.Count;
            return new Pose(point.position, point.rotation);
        }
    }
}
