using System;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    public interface IReadOnlyFlightFieldSceneModel {
        // The parent of every local structure of this peer (null until the first field); pooled ones stay under it inactive.
        public Transform Root { get; }
        public bool HasRoot { get; }
        // The live field number the local structures belong to; LiveFieldNumbers.NO_FIELD while none stands.
        public int BuiltNumber { get; }
        public int BuiltStructures { get; }
        // Every placement of BuiltNumber has been gone through; building runs over several frames.
        public bool IsBuilt { get; }

        public event Action OnChanged;
    }
}
