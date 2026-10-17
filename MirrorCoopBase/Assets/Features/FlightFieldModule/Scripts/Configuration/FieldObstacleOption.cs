using System;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // A giant obstacle a zone of a profile may use, with its relative chance among the options of the same footprint.
    [Serializable]
    public sealed class FieldObstacleOption : IFieldWeighted {
        [SerializeField] private FieldStructureDefinition _structure;
        [SerializeField, Min(0f)] private float _weight = 1f;

        public FieldStructureDefinition Structure => _structure;
        public float Weight => _weight;
    }
}
