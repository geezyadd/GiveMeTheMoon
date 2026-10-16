using System;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Structures {
    // One rock a cluster recipe may pick, with its relative chance.
    [Serializable]
    public sealed class FieldClusterRockOption {
        [Tooltip("A visual rock prefab: a FieldRockVisual at the root, a mesh about 1 m across centred on the pivot, no colliders.")]
        [SerializeField] private FieldRockVisual _prefab;
        [SerializeField, Min(0f)] private float _weight = 1f;

        public FieldRockVisual Prefab => _prefab;
        public float Weight => _weight;
    }
}
