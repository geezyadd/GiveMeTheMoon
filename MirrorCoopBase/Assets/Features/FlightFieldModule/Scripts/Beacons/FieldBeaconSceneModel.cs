using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Network;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Beacons {
    // What this peer has built of the field's beacons: the root, the field they belong to, all its beacons and the
    // ones standing now.
    public sealed class FieldBeaconSceneModel {
        private readonly List<FieldBeacon> _beacons = new();
        private readonly HashSet<int> _builtBeacons = new();

        public Transform Root { get; private set; }
        public bool HasRoot => Root != null;
        public int BuiltNumber { get; private set; } = LiveFieldNumbers.NO_FIELD;
        public IReadOnlyList<FieldBeacon> Beacons => _beacons;
        public IReadOnlyCollection<int> BuiltBeacons => _builtBeacons;

        public void SetRoot(Transform root) =>
            Root = root;

        public void BeginBuild(int number, IReadOnlyList<FieldBeacon> beacons) {
            BuiltNumber = number;
            _beacons.Clear();
            _beacons.AddRange(beacons);
            _builtBeacons.Clear();
        }

        public bool IsBeaconBuilt(int index) =>
            _builtBeacons.Contains(index);

        public void AddBeacon(int index) =>
            _builtBeacons.Add(index);

        public void RemoveBeacon(int index) =>
            _builtBeacons.Remove(index);

        public void Clear() {
            BuiltNumber = LiveFieldNumbers.NO_FIELD;
            _beacons.Clear();
            _builtBeacons.Clear();
        }
    }
}
