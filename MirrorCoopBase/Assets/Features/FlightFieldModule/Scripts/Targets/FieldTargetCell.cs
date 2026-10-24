using Features.FlightFieldModule.Scripts.Generation;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Targets {
    // A passable cell a target balloon may stand in, weighted by its distance from the main course.
    public readonly struct FieldTargetCell : IFieldWeighted {
        public Vector2Int Cell { get; }
        public float Weight { get; }

        public FieldTargetCell(Vector2Int cell, float weight) {
            Cell = cell;
            Weight = weight;
        }
    }
}
