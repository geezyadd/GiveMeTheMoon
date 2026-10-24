using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Targets {
    // A passable cell a target balloon may stand in, weighted by its distance from the main course (the step picks
    // over the running totals of the weights).
    public readonly struct FieldTargetCell {
        public Vector2Int Cell { get; }
        public float Weight { get; }

        public FieldTargetCell(Vector2Int cell, float weight) {
            Cell = cell;
            Weight = weight;
        }
    }
}
