using Features.FlightFieldModule.Scripts.Network;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Ore {
    // The ore of the live field as this peer has it: one entry per piece, in no particular order, and the field's
    // frame now (after every world shift) that turns field space into the world.
    public interface IFieldOres {
        public bool HasField { get; }
        public Vector3 Origin { get; }
        public Quaternion Rotation { get; }
        public int Count { get; }

        public FieldOreEntry Get(int index);
        public bool TryFindIndex(int id, out int index);
    }
}
