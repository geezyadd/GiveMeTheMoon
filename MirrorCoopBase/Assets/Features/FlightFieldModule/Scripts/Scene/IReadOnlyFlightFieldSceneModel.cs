using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    public interface IReadOnlyFlightFieldSceneModel {
        // The parent of every local structure of this peer (null until the first field); pooled ones stay under it inactive.
        public Transform Root { get; }
        public bool HasRoot { get; }
        // The parent of the small debris rocks, posed like Root (null until the first field).
        public Transform DebrisRoot { get; }
        // The live field number the local structures belong to; LiveFieldNumbers.NO_FIELD while none stands.
        public int BuiltNumber { get; }
        // Every placement of BuiltNumber within the build radius of the viewpoint stands; building runs over several frames.
        public bool IsBuilt { get; }
        // The live field's frame as the roots stand now: placements and the viewpoint are measured in it.
        public FieldFrame Frame { get; }
        // By placement index of the live field; the one record of which placements stand on this peer.
        public IReadOnlyList<FieldPlacementState> PlacementStates { get; }
    }
}
