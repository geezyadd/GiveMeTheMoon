using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Debris {
    public interface IFieldDebrisLayout {
        // The rocks of one debris cell from the field seed and the cells around it only, so every peer gets the same:
        // any two rocks of the field are at least the minimum gap apart and every rock keeps the ship gap from the
        // giants' cells. The workspace is the caller's (generation and building run on different threads).
        public void Fill(int fieldSeed, Vector2Int cell, float cellSize, IFieldDebrisCells cells, FieldDebrisWorkspace workspace, List<FieldDebrisSpot> spots);
    }
}
