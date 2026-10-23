using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // What this peer has built of the live field.
    public sealed class FlightFieldSceneModel : IReadOnlyFlightFieldSceneModel {
        private readonly List<FieldPlacementState> _placementStates = new();

        public Transform Root { get; private set; }
        public bool HasRoot => Root != null;
        // The small debris stands under its own root posed like Root, so Root keeps one object per structure.
        public Transform DebrisRoot { get; private set; }
        public int BuiltNumber { get; private set; }
        public bool IsBuilt { get; private set; }
        public FieldFrame Frame { get; private set; }
        public IReadOnlyList<FieldPlacementState> PlacementStates => _placementStates;

        public void SetRoots(Transform root, Transform debrisRoot) {
            Root = root;
            DebrisRoot = debrisRoot;
        }

        public void BeginBuild(int number) {
            BuiltNumber = number;
            IsBuilt = false;
        }

        public void CompleteBuild() =>
            IsBuilt = true;

        public void SetFrame(FieldFrame frame) =>
            Frame = frame;

        // New placements of the live field arrive in batches; each starts unbuilt.
        public void TrackPlacements(int count) {
            while (_placementStates.Count < count)
                _placementStates.Add(FieldPlacementState.NotBuilt);
        }

        public void SetPlacementState(int placementIndex, FieldPlacementState state) =>
            _placementStates[placementIndex] = state;

        public void Clear() {
            BuiltNumber = LiveFieldNumbers.NO_FIELD;
            IsBuilt = false;
            _placementStates.Clear();
        }
    }
}
