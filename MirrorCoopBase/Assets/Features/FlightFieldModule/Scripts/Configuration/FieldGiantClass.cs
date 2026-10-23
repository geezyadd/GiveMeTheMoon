using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // One size class of the giant rocks of an obstacle field (large, massive): placements in the matrix, synced, each
    // a single huge rock over its footprint. The class is picked by weight, then one of its obstacles by theirs.
    [Serializable]
    public sealed class FieldGiantClass : IFieldWeighted {
        [Tooltip("Shown in the inspector and in validation messages.")]
        [SerializeField] private string _name;
        [SerializeField, Min(0f)] private float _weight = 1f;
        [Tooltip("The giant structures of this class (their footprints give the size) and their relative chances.")]
        [SerializeField] private FieldObstacleOption[] _obstacles = Array.Empty<FieldObstacleOption>();

        public string Name => _name;
        public float Weight => _weight;
        public IReadOnlyList<FieldObstacleOption> Obstacles => _obstacles;
    }
}
