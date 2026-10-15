using System;
using Features.FlightFieldModule.Scripts.Network;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // What this peer has built of the live field.
    public sealed class FlightFieldSceneModel : IReadOnlyFlightFieldSceneModel {
        public Transform Root { get; private set; }
        public bool HasRoot => Root != null;
        public int BuiltNumber { get; private set; }
        public int BuiltStructures { get; private set; }
        public bool IsBuilt { get; private set; }

        public event Action OnChanged;

        public void SetRoot(Transform root) {
            Root = root;
            OnChanged?.Invoke();
        }

        public void BeginBuild(int number) {
            BuiltNumber = number;
            BuiltStructures = 0;
            IsBuilt = false;
            OnChanged?.Invoke();
        }

        public void AddBuiltStructure() {
            BuiltStructures++;
            OnChanged?.Invoke();
        }

        public void CompleteBuild() {
            IsBuilt = true;
            OnChanged?.Invoke();
        }

        public void Clear() {
            BuiltNumber = LiveFieldNumbers.NO_FIELD;
            BuiltStructures = 0;
            IsBuilt = false;
            OnChanged?.Invoke();
        }
    }
}
