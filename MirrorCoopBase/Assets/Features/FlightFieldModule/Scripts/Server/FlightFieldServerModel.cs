using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;
using Mirror;

namespace Features.FlightFieldModule.Scripts.Server {
    // The server's side of the live field: the generated field, its number and the Mirror objects spawned for it.
    // Peers only see the published LiveField model.
    public sealed class FlightFieldServerModel : IReadOnlyFlightFieldServerModel {
        private readonly List<NetworkIdentity> _networkedStructures = new();
        private int _publishedFields;

        public bool HasField => Field != null;
        public FlightField Field { get; private set; }
        public int Number { get; private set; }
        public IReadOnlyList<NetworkIdentity> NetworkedStructures => _networkedStructures;

        public void SetField(FlightField field) {
            _networkedStructures.Clear();
            _publishedFields++;
            Field = field;
            Number = _publishedFields;
        }

        public void AddNetworkedStructure(NetworkIdentity structure) =>
            _networkedStructures.Add(structure);

        public void ClearField() {
            _networkedStructures.Clear();
            Field = null;
            Number = LiveFieldNumbers.NO_FIELD;
        }
    }
}
