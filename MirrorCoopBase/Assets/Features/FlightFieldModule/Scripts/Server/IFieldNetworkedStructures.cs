using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Server {
    public interface IFieldNetworkedStructures {
        public void ServerSpawn(FlightFieldServerModel flightFieldServerModel);
        public void ServerDespawn(IReadOnlyFlightFieldServerModel flightFieldServerModel);
        public void ServerShift(IReadOnlyFlightFieldServerModel flightFieldServerModel, Vector3 delta);
        public void ClientShift(Vector3 delta);
    }
}
