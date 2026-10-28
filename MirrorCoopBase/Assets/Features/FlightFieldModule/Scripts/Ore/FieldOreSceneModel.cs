using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Ore {
    // This peer's shown pieces of ore: their root (kept across fields) and the view standing for each piece id.
    public sealed class FieldOreSceneModel {
        private readonly Dictionary<int, FieldOreView> _views = new();
        private readonly List<int> _ids = new();

        public Transform Root { get; private set; }
        public bool HasRoot => Root != null;
        public IReadOnlyList<int> Ids => _ids;

        public void SetRoot(Transform root) =>
            Root = root;

        public bool TryGetView(int id, out FieldOreView view) =>
            _views.TryGetValue(id, out view);

        public void Add(int id, FieldOreView view) {
            _views.Add(id, view);
            _ids.Add(id);
        }

        public void Remove(int id) {
            _views.Remove(id);
            _ids.Remove(id);
        }
    }
}
