using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Network;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Targets {
    // This peer's built target balloons of the live field: their root (posed like the field root, kept across
    // fields), which field number they belong to and the view standing for each balloon index (null = not built here).
    public sealed class FieldTargetSceneModel : IReadOnlyFieldTargetSceneModel {
        private readonly List<FieldTargetView> _views = new();

        public Transform Root { get; private set; }
        public bool HasRoot => Root != null;
        public int BuiltNumber { get; private set; } = LiveFieldNumbers.NO_FIELD;
        public IReadOnlyList<FieldTargetView> Views => _views;

        public void SetRoot(Transform root) =>
            Root = root;

        public void BeginBuild(int number) {
            _views.Clear();
            BuiltNumber = number;
        }

        // The list grows with the live field's target list; a new balloon starts unbuilt.
        public void TrackTargets(int count) {
            while (_views.Count < count)
                _views.Add(null);
        }

        public void SetView(int index, FieldTargetView view) =>
            _views[index] = view;

        public void Clear() {
            _views.Clear();
            BuiltNumber = LiveFieldNumbers.NO_FIELD;
        }

        public bool TryGetView(int index, out FieldTargetView view) {
            view = index >= 0 && index < _views.Count ? _views[index] : null;
            return view != null;
        }
    }
}
