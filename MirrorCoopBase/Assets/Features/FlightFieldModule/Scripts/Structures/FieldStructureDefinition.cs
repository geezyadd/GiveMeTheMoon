using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Structures {
    // The single description of one kind of field structure; generation and building read structures only through it.
    [CreateAssetMenu(menuName = "Game/Flight Field/Structure Definition", fileName = "FieldStructureDefinition")]
    public sealed class FieldStructureDefinition : ScriptableObject {
        [Tooltip("Stable id stored in field placements and sent over the network: never rename it once it is used.")]
        [SerializeField] private string _id;
        [Tooltip("Which generation step places this structure.")]
        [SerializeField] private FieldStructureKind _kind = FieldStructureKind.Obstacle;
        [Tooltip("Occupied cells, one string per row (row = cell across the field, character = cell along it): 'X' occupied, '.' free. Any shape, not only rectangles; all rows have the same length.")]
        [SerializeField] private string[] _footprintRows = Array.Empty<string>();
        [SerializeField] private GameObject _prefab;
        [Tooltip("A Mirror object the server spawns; otherwise every participant builds a local static object.")]
        [SerializeField] private bool _isNetworked;
        [Tooltip("Visual only: the built object turns by a random angle up to this many degrees either way; the footprint does not change.")]
        [SerializeField, Range(0f, 180f)] private float _visualYawJitterDegrees;
        [Tooltip("Visual only: the built object's scale differs by a random share up to this value either way; the footprint does not change.")]
        [SerializeField, Range(0f, 0.5f)] private float _visualScaleJitter;
        [Tooltip("Visual only: the rocks a local structure is built from; the prefab keeps only the trigger. Empty = the prefab is the whole look.")]
        [SerializeField] private FieldClusterRecipe _clusterRecipe;

        public string Id => _id;
        public FieldStructureKind Kind => _kind;
        public IReadOnlyList<string> FootprintRows => _footprintRows;
        public GameObject Prefab => _prefab;
        public bool IsNetworked => _isNetworked;
        public float VisualYawJitterDegrees => _visualYawJitterDegrees;
        public float VisualScaleJitter => _visualScaleJitter;
        public bool HasClusterRecipe => _clusterRecipe != null;
        public FieldClusterRecipe ClusterRecipe => _clusterRecipe;
    }
}
