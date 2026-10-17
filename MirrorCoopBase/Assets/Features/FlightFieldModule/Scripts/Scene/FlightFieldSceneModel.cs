using Features.FlightFieldModule.Scripts.Network;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // What this peer has built of the live field.
    public sealed class FlightFieldSceneModel : IReadOnlyFlightFieldSceneModel {
        public Transform Root { get; private set; }
        public bool HasRoot => Root != null;
        // The small debris stands under its own root posed like Root, so Root keeps one object per structure.
        public Transform DebrisRoot { get; private set; }
        public int BuiltNumber { get; private set; }
        public bool IsBuilt { get; private set; }

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

        public void Clear() {
            BuiltNumber = LiveFieldNumbers.NO_FIELD;
            IsBuilt = false;
        }
    }
}
