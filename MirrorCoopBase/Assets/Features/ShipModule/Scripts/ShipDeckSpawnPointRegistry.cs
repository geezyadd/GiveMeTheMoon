using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // The deck spawn points of the ship in the loaded map, handed out in turn.
    public sealed class ShipDeckSpawnPointRegistry : IShipDeckSpawnPointRegistry, IShipDeckSpawnPoints {
        private const string NO_POINT_ERROR = "The ship has no " + nameof(ShipDeckSpawnPoint) + ".";

        private readonly List<ShipDeckSpawnPoint> _points = new();
        private int _nextIndex;

        public bool HasSpawnPoint =>
            _points.Count > 0;

        public void Add(ShipDeckSpawnPoint point) {
            if (_points.Contains(point) == false)
                _points.Add(point);
        }

        public void Remove(ShipDeckSpawnPoint point) =>
            _points.Remove(point);

        public Pose TakeSpawnPose() {
            if (HasSpawnPoint == false)
                throw new InvalidOperationException(NO_POINT_ERROR);

            Transform point = _points[_nextIndex % _points.Count].transform;
            _nextIndex = (_nextIndex + 1) % _points.Count;
            return new Pose(point.position, point.rotation);
        }
    }
}
