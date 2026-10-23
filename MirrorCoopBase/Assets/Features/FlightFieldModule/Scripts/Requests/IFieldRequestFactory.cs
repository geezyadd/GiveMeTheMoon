using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Requests {
    public interface IFieldRequestFactory {
        public FieldRequest Create(Vector3 stationA, Vector3 stationB, Vector3 cruiseStart, int loopIndex, int seed);
    }
}
