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
        [Tooltip("Relative chance to be picked by a random fill; 0 = never picked by chance (e.g. wall only).")]
        [SerializeField, Min(0f)] private float _weight = 1f;
        [Tooltip("May be turned by 90 degrees when placed.")]
        [SerializeField] private bool _isRotatable = true;

        public string Id => _id;
        public FieldStructureKind Kind => _kind;
        public IReadOnlyList<string> FootprintRows => _footprintRows;
        public GameObject Prefab => _prefab;
        public bool IsNetworked => _isNetworked;
        public float Weight => _weight;
        public bool IsRotatable => _isRotatable;
    }
}
