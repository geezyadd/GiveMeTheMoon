using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    public interface IFieldDecorPlanetPicker {
        // The course start is the start of the route axis in field space: planets stand around the route and keep
        // clear of the course ahead as seen from there. The field's half width (this flight's, across the route axis)
        // keeps a planet beside the field out of it.
        public void Fill(int fieldSeed, Vector3 courseStart, float fieldHalfWidth, List<FieldDecorPlanet> planets);
        // The light's travel direction is in field space; within the picked side the planet turns towards it.
        public void Fill(int fieldSeed, Vector3 courseStart, float fieldHalfWidth, Vector3 lightTravel, List<FieldDecorPlanet> planets);
    }
}
