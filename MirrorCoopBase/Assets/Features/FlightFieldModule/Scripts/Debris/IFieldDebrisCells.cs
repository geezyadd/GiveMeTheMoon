using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Debris {
    // What the debris layout needs to know about the field around a cell: which cells hold debris (density and kind)
    // and which are closed by a structure; every other cell (also outside the field) is empty and stays so; and the
    // field's profile (an obstacle field sizes its rocks by the profile's classes). The generator reads its matrix, a
    // peer the live field.
    public interface IFieldDebrisCells {
        public FieldDifficultyProfile Profile { get; }
        public bool TryGetDebris(Vector2Int cell, out float density, out FieldDebrisRecipe recipe);
        public bool IsBlocked(Vector2Int cell);
        public bool IsEmpty(Vector2Int cell);
    }
}
