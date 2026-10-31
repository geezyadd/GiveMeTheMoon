using System.Collections.Generic;
using Features.PointOfInterestModule.Scripts.Network;

namespace Features.PointOfInterestModule.Scripts.Editor.Tests {
    // The synced point list without a network bridge.
    public sealed class RecordingPointsOfInterest : IPointsOfInterest, IPointOfInterestWriter {
        private readonly List<PointOfInterestEntry> _points = new();

        public int Count => _points.Count;
        public bool IsAvailable => true;

        public PointOfInterestEntry Get(int index) =>
            _points[index];

        public bool TryFindIndex(int id, out int index) {
            index = _points.FindIndex(point => point.Id == id);
            return index >= 0;
        }

        public void ServerAdd(PointOfInterestEntry entry) =>
            _points.Add(entry);

        public void ServerSet(int index, PointOfInterestEntry entry) =>
            _points[index] = entry;

        public void ServerClear() =>
            _points.Clear();
    }
}
