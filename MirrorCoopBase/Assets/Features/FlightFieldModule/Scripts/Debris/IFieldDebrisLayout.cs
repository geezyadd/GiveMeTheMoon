using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Debris {
    public interface IFieldDebrisLayout {
        // The rocks of one debris cell from the field seed and the cell only, so every peer gets the same; any two
        // rocks, in this cell or a neighbouring one, are at least the minimum gap apart. The random sequence is the
        // caller's (generation and building run on different threads).
        public void Fill(FieldDebrisRecipe recipe, float density, int fieldSeed, Vector2Int cell, float cellSize, float minGap, IFieldRandom random,
                         List<FieldDebrisSpot> spots);
        // The seed of the cell's look (which rock, material, turn) once its spots are laid out.
        public int LookSeed(int fieldSeed, Vector2Int cell);
    }
}
