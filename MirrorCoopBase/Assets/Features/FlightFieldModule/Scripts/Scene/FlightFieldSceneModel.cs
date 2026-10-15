using Features.FlightFieldModule.Scripts.Network;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // What this peer has built of the live field.
    public sealed class FlightFieldSceneModel : IReadOnlyFlightFieldSceneModel {
        public Transform Root { get; private set; }
        public bool HasRoot => Root != null;
        public int BuiltNumber { get; private set; }
        public bool IsBuilt { get; private set; }

        public void SetRoot(Transform root) =>
            Root = root;

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
