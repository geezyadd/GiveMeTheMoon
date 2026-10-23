using System;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Network {
    // A run of debris cells along one row of the field (cells Start.x .. Start.x + Length - 1 at Start.y) with the
    // same debris kind and density: every peer lays the small rocks of these cells out itself, so the network carries
    // one entry per run, not one per rock.
    [Serializable]
    public struct FieldDebrisRunEntry {
        public Vector2Int Start;
        public int Length;
        public float Density;
        public string DebrisId;
    }
}
