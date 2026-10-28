using System;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // What a broken rock of one size drops: with the chance it drops anything, then a count between the bounds.
    [Serializable]
    public sealed class FieldOreDrop {
        [Tooltip("The chance (0..1) that a broken rock of this size drops any ore.")]
        [SerializeField, Range(0f, 1f)] private float _chance;
        [Tooltip("The fewest pieces it drops when it drops any.")]
        [SerializeField, Min(0)] private int _minCount;
        [Tooltip("The most pieces it drops.")]
        [SerializeField, Min(0)] private int _maxCount;

        public float Chance => _chance;
        public int MinCount => _minCount;
        public int MaxCount => _maxCount;
    }
}
