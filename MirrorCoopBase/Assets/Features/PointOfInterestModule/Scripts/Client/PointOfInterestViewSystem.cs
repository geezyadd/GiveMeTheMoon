using System;
using System.Collections.Generic;
using Features.PointOfInterestModule.Scripts.Configuration;
using Features.PointOfInterestModule.Scripts.Network;
using Features.PointOfInterestModule.Scripts.Network.Generated;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Features.PointOfInterestModule.Scripts.Client {
    // Every peer: one look per standing point of the synced list; an opened container or a point gone with its field
    // takes its look away, a moved point (a world shift) moves its look. One to three points per flight, so the looks
    // are made and destroyed, not pooled.
    public sealed class PointOfInterestViewSystem : IInitializable, IDisposable {
        private readonly IReadOnlyPointsOfInterestModel _pointsOfInterestModel;
        private readonly IPointsOfInterest _pointsOfInterest;
        private readonly IPointOfInterestContentCatalog _pointOfInterestContentCatalog;
        private readonly PointOfInterestSceneModel _pointOfInterestSceneModel;
        private readonly IInstantiator _instantiator;
        private readonly List<int> _goneIds = new();

        public PointOfInterestViewSystem(IReadOnlyPointsOfInterestModel pointsOfInterestModel, IPointsOfInterest pointsOfInterest,
                                         IPointOfInterestContentCatalog pointOfInterestContentCatalog, PointOfInterestSceneModel pointOfInterestSceneModel,
                                         IInstantiator instantiator) {
            _pointsOfInterestModel = pointsOfInterestModel;
            _pointsOfInterest = pointsOfInterest;
            _pointOfInterestContentCatalog = pointOfInterestContentCatalog;
            _pointOfInterestSceneModel = pointOfInterestSceneModel;
            _instantiator = instantiator;
        }

        public void Initialize() {
            _pointsOfInterestModel.OnChanged += OnPointsChanged;
            _pointsOfInterestModel.OnAvailableChanged += OnPointsChanged;
        }

        public void Dispose() {
            _pointsOfInterestModel.OnChanged -= OnPointsChanged;
            _pointsOfInterestModel.OnAvailableChanged -= OnPointsChanged;
            RemoveViewsExcept(Array.Empty<int>());
        }

        private void OnPointsChanged() {
            List<int> standing = new();
            for (int i = 0; i < _pointsOfInterest.Count; i++) {
                PointOfInterestEntry point = _pointsOfInterest.Get(i);
                if (point.State == PointOfInterestState.Opened)
                    continue;

                standing.Add(point.Id);
                if (_pointOfInterestSceneModel.TryGetView(point.Id, out PointOfInterestView view))
                    view.Show(point.Id, point.Position);
                else
                    CreateView(point);
            }

            RemoveViewsExcept(standing);
        }

        private void CreateView(in PointOfInterestEntry point) {
            PointOfInterestView prefab = _pointOfInterestContentCatalog.GetRequired(point.ContentId).ViewPrefab;
            PointOfInterestView view = _instantiator.InstantiatePrefabForComponent<PointOfInterestView>(prefab);
            view.Show(point.Id, point.Position);
            _pointOfInterestSceneModel.Add(point.Id, view);
        }

        private void RemoveViewsExcept(IReadOnlyCollection<int> standing) {
            _goneIds.Clear();
            foreach (int id in _pointOfInterestSceneModel.Views.Keys) {
                if (!Contains(standing, id))
                    _goneIds.Add(id);
            }

            foreach (int id in _goneIds) {
                if (_pointOfInterestSceneModel.TryGetView(id, out PointOfInterestView view) && view != null)
                    Object.Destroy(view.gameObject);

                _pointOfInterestSceneModel.Remove(id);
            }
        }

        private static bool Contains(IReadOnlyCollection<int> ids, int wanted) {
            foreach (int id in ids) {
                if (id == wanted)
                    return true;
            }

            return false;
        }
    }
}
