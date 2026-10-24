using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // One rock of a cluster: which prefab and material, and its pose relative to the structure's field position
    // (field axes, not turned with the structure).
    public readonly struct FieldClusterRockPose {
        public FieldRockVisual Prefab { get; }
        public Material Material { get; }
        public Vector3 Offset { get; }
        public Quaternion Rotation { get; }
        public Vector3 Scale { get; }
        // A rock that closes the flight plane over footprint cells; it may reach a little past the footprint.
        public bool IsPlaneCover { get; }
        // The rock's mesh stays within this diameter around its offset however it is turned.
        public float BoundingDiameter => 2f * FieldRockVisual.MESH_REACH * Mathf.Max(Scale.x, Mathf.Max(Scale.y, Scale.z));

        public FieldClusterRockPose(FieldRockVisual prefab, Material material, Vector3 offset, Quaternion rotation, Vector3 scale)
            : this(prefab, material, offset, rotation, scale, false) { }

        public FieldClusterRockPose(FieldRockVisual prefab, Material material, Vector3 offset, Quaternion rotation, Vector3 scale, bool isPlaneCover) {
            Prefab = prefab;
            Material = material;
            Offset = offset;
            Rotation = rotation;
            Scale = scale;
            IsPlaneCover = isPlaneCover;
        }
    }
}
