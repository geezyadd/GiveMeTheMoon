using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Network {
    public interface IFlightFieldPublisher {
        public void Publish(FlightField field, int number);
        public void Clear();
        public void ShiftOrigin(Vector3 delta);
    }
}
