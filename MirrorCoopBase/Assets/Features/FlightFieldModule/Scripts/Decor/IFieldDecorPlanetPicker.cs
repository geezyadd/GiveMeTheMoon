using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    public interface IFieldDecorPlanetPicker {
        public void Fill(int fieldSeed, List<FieldDecorPlanet> planets);
        // The light's travel direction is in field space; the planets go to the side of the sky the light points to.
        public void Fill(int fieldSeed, Vector3 lightTravel, List<FieldDecorPlanet> planets);
    }
}
