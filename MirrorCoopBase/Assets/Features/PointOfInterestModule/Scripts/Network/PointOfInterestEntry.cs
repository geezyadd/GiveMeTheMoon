using System;
using UnityEngine;

namespace Features.PointOfInterestModule.Scripts.Network {
    // One point of interest as every peer sees it: its id in this field, what is in it (a content id of the
    // configuration), the middle of its zone and reward container in the world and how far the crew got with it.
    [Serializable]
    public struct PointOfInterestEntry {
        public int Id;
        public string ContentId;
        public Vector3 Position;
        public PointOfInterestState State;
    }
}
