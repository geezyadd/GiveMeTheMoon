using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Generation;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Ore {
    // Server: drops the ore of a broken rock (as many pieces as its size rolls) or a given number of pieces at a point,
    // of kinds picked by their spawn weights, each drifting away from the middle. Returns how many pieces dropped.
    public interface IFieldOreDropService {
        public int ServerDropFromRock(in FieldRockDescription rock, IFieldRandom random);
        public int ServerDropAt(Vector3 position, float scatterRadius, int count, IFieldRandom random);
    }
}
