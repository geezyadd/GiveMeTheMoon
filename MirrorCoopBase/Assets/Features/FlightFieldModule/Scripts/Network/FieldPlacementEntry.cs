using System;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Network {
    // One placement as it travels over the network: every peer builds the structure from it, nobody generates again.
    [Serializable]
    public struct FieldPlacementEntry {
        public string StructureId;
        public Vector2Int Anchor;
        public FieldRotation Rotation;
    }
}
