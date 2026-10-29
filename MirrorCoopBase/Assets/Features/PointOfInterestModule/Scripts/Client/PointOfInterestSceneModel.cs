using System.Collections.Generic;

namespace Features.PointOfInterestModule.Scripts.Client {
    // The looks this peer shows, by point id: a standing reward container has one, an opened one none.
    public sealed class PointOfInterestSceneModel {
        private readonly Dictionary<int, PointOfInterestView> _views = new();

        public IReadOnlyDictionary<int, PointOfInterestView> Views => _views;

        public bool TryGetView(int pointId, out PointOfInterestView view) =>
            _views.TryGetValue(pointId, out view);

        public void Add(int pointId, PointOfInterestView view) =>
            _views.Add(pointId, view);

        public void Remove(int pointId) =>
            _views.Remove(pointId);
    }
}
