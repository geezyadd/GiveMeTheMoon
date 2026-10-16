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

        public FieldClusterRockPose(FieldRockVisual prefab, Material material, Vector3 offset, Quaternion rotation, Vector3 scale) {
            Prefab = prefab;
            Material = material;
            Offset = offset;
            Rotation = rotation;
            Scale = scale;
        }
    }
}
