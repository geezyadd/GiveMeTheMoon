using System;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Network {
    // One target balloon as it travels over the network: its kind and its middle in field space. Every peer builds the
    // balloon from it; its index in the list is its FieldTargetId.
    [Serializable]
    public struct FieldTargetEntry {
        public string KindId;
        public Vector3 FieldPosition;
    }
}
