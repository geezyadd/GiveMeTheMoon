using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    public interface IFieldDebrisSceneBuilder {
        public Transform CreateRoot();
        // Builds the rocks of one debris cell under the root (posed like the field root).
        public void BuildCell(Transform root, int fieldSeed, float cellSize, Vector2Int cell, IFieldDebrisCells cells);
        // A rock of a debris cell built on this peer (hidden when broken); false when the cell is not built here.
        public bool TryGetBuiltRock(Vector2Int cell, int rockIndex, out FieldRockVisual rock);
        // Returns the rocks of the cell to the pool; nothing happens when none were built.
        public void ReleaseCell(Vector2Int cell);
        public void ReleaseAll();
    }
}
